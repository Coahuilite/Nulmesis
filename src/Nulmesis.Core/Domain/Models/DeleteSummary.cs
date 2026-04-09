namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Summary of a delete operation.
/// </summary>
public sealed class DeleteSummary
{
    public required int RequestedCount { get; init; }
    public required int DeletedCount { get; init; }
    public required int FailedCount { get; init; }
    public required bool Cancelled { get; init; }

    public DeleteSummary() { }
}
