namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Result of a delete operation.
/// </summary>
public sealed class DeleteResult
{
    public required DeleteSummary Summary { get; init; }
    public required List<DeleteError> Errors { get; init; }

    public DeleteResult() { }
}
