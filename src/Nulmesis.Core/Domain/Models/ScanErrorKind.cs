namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Category for errors encountered during a scan operation.
/// </summary>
public enum ScanErrorKind
{
    Unknown,
    AccessDenied,
    ReparsePointSkipped,
    IoFailure,
    DirectoryNotFound
}
