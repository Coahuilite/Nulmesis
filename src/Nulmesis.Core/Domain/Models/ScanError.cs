namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Error encountered during a scan operation.
/// </summary>
public sealed class ScanError
{
    public required ScanErrorKind Kind { get; init; }
    public required string Path { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }

    public ScanError() { }
}
