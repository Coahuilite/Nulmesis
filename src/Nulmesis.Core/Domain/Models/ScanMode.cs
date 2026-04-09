namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Scan mode determines how strictly reserved names are matched.
/// </summary>
public enum ScanMode
{
    /// <summary>
    /// Strict mode: reserved name must be an exact match and have zero length.
    /// </summary>
    Strict,

    /// <summary>
    /// Loose mode: reserved name match is case-insensitive, size is not checked.
    /// </summary>
    Loose
}
