namespace GnuCash.DotNet.Bridge.Native;

internal sealed class GnuCashNativeOperationException : Exception
{
    public GnuCashNativeOperationException(
        string message,
        int? backendErrorCode = null,
        string? backendErrorMessage = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        BackendErrorCode = backendErrorCode;
        BackendErrorMessage = backendErrorMessage;
    }

    public int? BackendErrorCode { get; }

    public string? BackendErrorMessage { get; }
}
