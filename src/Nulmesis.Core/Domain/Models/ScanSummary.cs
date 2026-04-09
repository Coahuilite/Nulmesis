namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Summary of a scan operation.
/// </summary>
public sealed class ScanSummary
{
    public required string Root { get; init; }
    public required ScanMode Mode { get; init; }
    public required int MatchedCount { get; init; }
    public required int ErrorCount { get; init; }
    public required long DurationMs { get; init; }

    public ScanSummary() { }
}
