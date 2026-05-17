namespace GnuCash.DotNet.Options;

/// <summary>
/// Selects how SDK book reads are served by the bridge process.
/// </summary>
public enum GnuCashBookReadMode
{
    Xml = 0,
    Native = 1,
    NativeThenXml = 2
}
