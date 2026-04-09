using System.Text.RegularExpressions;

namespace Nulmesis.Core.Domain.Normalizers;

/// <summary>
/// Normalizes file paths to Windows extended-length path format for consistent handling.
/// </summary>
public static partial class ReservedPathNormalizer
{
    private const string ExtendedPrefix = @"\\?\";
    private const string UncPrefix = @"\\";

    [GeneratedRegex(@"^\\\\\?\\", RegexOptions.Compiled)]
    private static partial Regex ExtendedPrefixRegex();

    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (ExtendedPrefixRegex().IsMatch(path))
            return path;

        if (path.StartsWith(UncPrefix, StringComparison.Ordinal))
            return ExtendedPrefix + @"UNC\" + path[2..];

        if (path.Length >= 2 && path[1] == ':')
            return ExtendedPrefix + path;

        return path;
    }
}
