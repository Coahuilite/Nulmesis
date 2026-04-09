using Xunit;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Core.Domain.Policies;

namespace Nulmesis.Core.Tests;

public class NulMatchPolicyTests
{
    private static NulMatch Make(string fileName, long size = 0) => new()
    {
        AbsolutePath = $@"C:\test\{fileName}",
        RelativePath = fileName,
        FileName = fileName,
        SizeBytes = size,
        LastWriteTimeUtc = DateTime.UtcNow
    };

    [Theory]
    [InlineData("nul")]
    [InlineData("NUL")]
    [InlineData("Nul")]
    public void IsMatch_Strict_ZeroSize_Matches(string fileName)
    {
        var match = Make(fileName, size: 0);
        Assert.True(NulMatchPolicy.IsMatch(match, ScanMode.Strict));
    }

    [Theory]
    [InlineData("nul")]
    [InlineData("NUL")]
    [InlineData("Nul")]
    [InlineData("NUL.TXT")]
    [InlineData("nul.txt")]
    public void IsMatch_Strict_NonZeroSize_NoMatch(string fileName)
    {
        var match = Make(fileName, size: 1);
        Assert.False(NulMatchPolicy.IsMatch(match, ScanMode.Strict));
    }

    [Theory]
    [InlineData("nul")]
    [InlineData("NUL")]
    [InlineData("Nul")]
    public void IsMatch_Loose_AnySize_Matches(string fileName)
    {
        var match = Make(fileName, size: 100);
        Assert.True(NulMatchPolicy.IsMatch(match, ScanMode.Loose));
    }

    [Theory]
    [InlineData("nul.txt")]
    [InlineData("nul.")]
    [InlineData("nul ")]
    [InlineData("NUL.TXT")]
    [InlineData("NUL.")]
    [InlineData("NUL ")]
    public void IsDisqualified_NeverMatches(string fileName)
    {
        Assert.True(NulMatchPolicy.IsDisqualified(fileName));
    }

    [Theory]
    [InlineData("nul")]
    [InlineData("file.txt")]
    [InlineData("data.csv")]
    public void IsDisqualified_OtherFiles_NoMatch(string fileName)
    {
        Assert.False(NulMatchPolicy.IsDisqualified(fileName));
    }

    [Theory]
    [InlineData("other")]
    [InlineData("com1")]
    [InlineData("aux")]
    [InlineData("prn")]
    public void IsMatch_Strict_NonNulName_NoMatch(string fileName)
    {
        var match = Make(fileName);
        Assert.False(NulMatchPolicy.IsMatch(match, ScanMode.Strict));
        Assert.False(NulMatchPolicy.IsMatch(match, ScanMode.Loose));
    }
}

public class ReservedPathNormalizerTests
{
    [Theory]
    [InlineData(@"C:\file.txt", @"\\?\C:\file.txt")]
    [InlineData(@"D:\data\file.txt", @"\\?\D:\data\file.txt")]
    public void Normalize_LocalPath_AddsExtendedPrefix(string input, string expected)
    {
        Assert.Equal(expected, ReservedPathNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(@"\\server\share\file.txt", @"\\?\UNC\server\share\file.txt")]
    [InlineData(@"\\machine\c$\windows\file.txt", @"\\?\UNC\machine\c$\windows\file.txt")]
    public void Normalize_UncPath_ConvertsToExtendedUnc(string input, string expected)
    {
        Assert.Equal(expected, ReservedPathNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(@"\\?\C:\file.txt")]
    [InlineData(@"\\?\UNC\server\share\file.txt")]
    public void Normalize_AlreadyExtended_NoChange(string input)
    {
        Assert.Equal(input, ReservedPathNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_EmptyOrNull_ReturnsAsIs()
    {
        Assert.Equal(string.Empty, ReservedPathNormalizer.Normalize(string.Empty));
        Assert.Null(ReservedPathNormalizer.Normalize(null!));
    }

    [Theory]
    [InlineData(@"relative\path\file.txt")]
    [InlineData(@"justfilename.txt")]
    public void Normalize_RelativePath_NoChange(string input)
    {
        Assert.Equal(input, ReservedPathNormalizer.Normalize(input));
    }
}
