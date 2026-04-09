namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Aggregated result of a scan operation.
/// </summary>
public sealed class ScanResult
{
    public required List<NulMatch> Matches { get; init; }
    public required List<NulMatch> DeleteTargets { get; init; }
    public required List<ScanError> Errors { get; init; }
    public required ScanSummary Summary { get; init; }

    public ScanResult() { }
}
