namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Shared delete confirmation payload for CLI and GUI flows.
/// </summary>
public sealed class DeleteConfirmationDto
{
    public required string Root { get; init; }
    public required ScanMode Mode { get; init; }
    public required int MatchedCount { get; init; }
    public required List<string> PreviewPaths { get; init; }

    public DeleteConfirmationDto() { }
}
