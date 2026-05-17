using System.Collections.ObjectModel;

namespace GnuCash.DotNet.Bridge.Native;

internal sealed class PortableExecutableExportReader
{
    public IReadOnlySet<string> ReadExportNames(string dllPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dllPath);

        using var stream = File.OpenRead(dllPath);
        using var reader = new BinaryReader(stream);

        ValidateDosHeader(reader);
        var peHeaderOffset = ReadInt32At(reader, 0x3C);
        ValidatePeHeader(reader, peHeaderOffset);

        var sectionCount = ReadUInt16At(reader, peHeaderOffset + 6);
        var optionalHeaderSize = ReadUInt16At(reader, peHeaderOffset + 20);
        var optionalHeaderOffset = peHeaderOffset + 24;
        var optionalHeaderMagic = ReadUInt16At(reader, optionalHeaderOffset);
        var dataDirectoryOffset = optionalHeaderMagic switch
        {
            0x10B => optionalHeaderOffset + 96,
            0x20B => optionalHeaderOffset + 112,
            _ => throw new InvalidDataException("The file is not a supported PE32 or PE32+ image.")
        };

        var exportTableRva = ReadUInt32At(reader, dataDirectoryOffset);
        if (exportTableRva == 0)
        {
            return new ReadOnlySet<string>(new HashSet<string>(StringComparer.Ordinal));
        }

        var sectionHeadersOffset = optionalHeaderOffset + optionalHeaderSize;
        var sections = ReadSections(reader, sectionHeadersOffset, sectionCount);
        var exportDirectoryOffset = RvaToOffset(sections, exportTableRva);
        var numberOfNames = ReadUInt32At(reader, exportDirectoryOffset + 24);
        var addressOfNamesRva = ReadUInt32At(reader, exportDirectoryOffset + 32);
        var addressOfNamesOffset = RvaToOffset(sections, addressOfNamesRva);
        var names = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < numberOfNames; i++)
        {
            var nameRva = ReadUInt32At(reader, addressOfNamesOffset + (i * sizeof(uint)));
            var nameOffset = RvaToOffset(sections, nameRva);
            names.Add(ReadNullTerminatedAscii(reader, nameOffset));
        }

        return new ReadOnlySet<string>(names);
    }

    private static IReadOnlyList<PeSection> ReadSections(
        BinaryReader reader,
        long sectionHeadersOffset,
        ushort sectionCount)
    {
        var sections = new List<PeSection>(sectionCount);
        for (var i = 0; i < sectionCount; i++)
        {
            var offset = sectionHeadersOffset + (i * 40);
            sections.Add(new PeSection(
                ReadUInt32At(reader, offset + 8),
                ReadUInt32At(reader, offset + 12),
                ReadUInt32At(reader, offset + 16),
                ReadUInt32At(reader, offset + 20)));
        }

        return sections;
    }

    private static long RvaToOffset(IReadOnlyList<PeSection> sections, uint rva)
    {
        foreach (var section in sections)
        {
            var sectionSize = Math.Max(section.VirtualSize, section.SizeOfRawData);
            if (rva >= section.VirtualAddress && rva < section.VirtualAddress + sectionSize)
            {
                return section.PointerToRawData + (rva - section.VirtualAddress);
            }
        }

        throw new InvalidDataException($"The PE RVA 0x{rva:X8} does not map to a file offset.");
    }

    private static void ValidateDosHeader(BinaryReader reader)
    {
        if (ReadUInt16At(reader, 0) != 0x5A4D)
        {
            throw new InvalidDataException("The file does not have a DOS MZ header.");
        }
    }

    private static void ValidatePeHeader(BinaryReader reader, long peHeaderOffset)
    {
        if (ReadUInt32At(reader, peHeaderOffset) != 0x00004550)
        {
            throw new InvalidDataException("The file does not have a PE header.");
        }
    }

    private static ushort ReadUInt16At(BinaryReader reader, long offset)
    {
        reader.BaseStream.Position = offset;
        return reader.ReadUInt16();
    }

    private static uint ReadUInt32At(BinaryReader reader, long offset)
    {
        reader.BaseStream.Position = offset;
        return reader.ReadUInt32();
    }

    private static int ReadInt32At(BinaryReader reader, long offset)
    {
        reader.BaseStream.Position = offset;
        return reader.ReadInt32();
    }

    private static string ReadNullTerminatedAscii(BinaryReader reader, long offset)
    {
        reader.BaseStream.Position = offset;
        var bytes = new List<byte>();
        byte current;
        while ((current = reader.ReadByte()) != 0)
        {
            bytes.Add(current);
        }

        return System.Text.Encoding.ASCII.GetString([.. bytes]);
    }

    private sealed record PeSection(
        uint VirtualSize,
        uint VirtualAddress,
        uint SizeOfRawData,
        uint PointerToRawData);
}
