namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Represents a file candidate matched as a reserved Windows name.
/// </summary>
public sealed class NulMatch
{
    public required string AbsolutePath { get; init; }
    public required string RelativePath { get; init; }
    public required string FileName { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime LastWriteTimeUtc { get; init; }

    public NulMatch() { }
}
