using Nulmesis.Core.Domain.Models;

namespace Nulmesis.Core.Domain.Policies;

/// <summary>
/// Policy for matching Windows reserved filename "nul".
/// </summary>
public static class NulMatchPolicy
{
    private const string NulBaseName = "nul";

    public static bool IsMatch(NulMatch candidate, ScanMode mode)
    {
        var name = Path.GetFileName(candidate.FileName);

        if (name.Equals(NulBaseName, StringComparison.OrdinalIgnoreCase))
        {
            if (mode == ScanMode.Strict)
                return candidate.SizeBytes == 0;
            return true;
        }

        return false;
    }

    public static bool IsDisqualified(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return name.Equals(NulBaseName + ".txt", StringComparison.OrdinalIgnoreCase)
            || name.Equals(NulBaseName + ".", StringComparison.OrdinalIgnoreCase)
            || name.Equals(NulBaseName + " ", StringComparison.OrdinalIgnoreCase);
    }
}
