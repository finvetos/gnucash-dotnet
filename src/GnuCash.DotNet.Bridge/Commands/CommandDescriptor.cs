namespace GnuCash.DotNet.Bridge.Commands;

/// <summary>
/// Describes a command for help and catalog output.
/// </summary>
public sealed record CommandDescriptor(string Name, string Description, string Usage);
