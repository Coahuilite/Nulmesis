using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Core.Services;
using Nulmesis.Tests.Shared;

namespace Nulmesis.Core.Tests;

public class NulFileScannerTests
{
    [Fact]
    public async Task ScanAsync_EmptyDirectory_ReturnsEmptyResult()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        var scanner = new NulFileScanner();

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        Assert.Empty(result.Matches);
        Assert.Empty(result.DeleteTargets);
        Assert.Empty(result.Errors);
        Assert.Equal(testRoot.RootPath, result.Summary.Root);
        Assert.Equal(0, result.Summary.MatchedCount);
    }

    [Fact]
    public async Task ScanAsync_DeepDirectoryTree_FindsNestedNul()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var deepestDirectory = fixture.CreateNestedTree(depth: 55, filesPerLevel: 1);
        var nulPath = fixture.CreateReadOnlyNulFile(Path.Combine(deepestDirectory, "nul"));
        var scanner = new NulFileScanner();

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        var match = Assert.Single(result.Matches);
        Assert.EndsWith(Path.Combine("level-55-cccccccccccccccccccccccccccccccc", "nul"), match.RelativePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("level-55-cccccccccccccccccccccccccccccccc", "nul"), match.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.True(match.AbsolutePath.Length > 260);
        Assert.DoesNotContain(@"\\?\", match.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\\.\", match.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanAsync_IsCaseInsensitiveForNulName()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        WriteReservedFile(testRoot.RootPath, "NUL", []);
        WriteReservedFile(Path.Combine(testRoot.RootPath, "alpha"), "nul", []);
        WriteReservedFile(Path.Combine(testRoot.RootPath, "beta"), "Nul", []);
        var scanner = new NulFileScanner();

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        Assert.Equal(3, result.Matches.Count);
        Assert.All(result.Matches, match => Assert.Equal("nul", match.FileName, ignoreCase: true));
    }

    [Fact]
    public async Task ScanAsync_StrictMode_OnlyCountsZeroByteFiles()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        WriteReservedFile(Path.Combine(testRoot.RootPath, "strict-zero"), "nul", []);
        WriteReservedFile(Path.Combine(testRoot.RootPath, "strict-nonzero"), "NUL", [1, 2, 3]);
        var scanner = new NulFileScanner();

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        var match = Assert.Single(result.Matches);
        Assert.Equal(Path.Combine("strict-zero", "nul"), match.RelativePath);
    }

    [Fact]
    public async Task ScanAsync_LooseMode_CountsAnyFileSize()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        WriteReservedFile(Path.Combine(testRoot.RootPath, "loose-zero"), "nul", []);
        WriteReservedFile(Path.Combine(testRoot.RootPath, "loose-nonzero"), "NUL", [1, 2, 3]);
        var scanner = new NulFileScanner();

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Loose, CancellationToken.None);

        Assert.Equal(2, result.Matches.Count);
    }

    [Fact]
    public async Task ScanAsync_AccessDenied_IsAggregatedInsteadOfThrown()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        var blockedDirectory = Path.Combine(testRoot.RootPath, "blocked");
        Directory.CreateDirectory(ReservedPathNormalizer.Normalize(blockedDirectory));
        WriteReservedFile(Path.Combine(testRoot.RootPath, "ok"), "nul", []);
        var scanner = new AccessDeniedTestScanner(blockedDirectory);

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        Assert.Single(result.Matches);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ScanErrorKind.AccessDenied, error.Kind);
        Assert.Equal(blockedDirectory, error.Path);
    }

    [Fact]
    public async Task ScanAsync_ReparsePointDirectory_IsSkippedAndReported()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        var reparseDirectory = Path.Combine(testRoot.RootPath, "junction-like");
        Directory.CreateDirectory(ReservedPathNormalizer.Normalize(reparseDirectory));
        WriteReservedFile(reparseDirectory, "nul", []);
        var scanner = new ReparsePointTestScanner(reparseDirectory);

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        Assert.Empty(result.Matches);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ScanErrorKind.ReparsePointSkipped, error.Kind);
        Assert.Equal(reparseDirectory, error.Path);
    }

    [Fact]
    public async Task ScanAsync_LongPath_DoesNotCrash()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var deepestDirectory = fixture.CreateNestedTree(depth: 6, filesPerLevel: 2);
        var nulPath = fixture.CreateReadOnlyNulFile(Path.Combine(deepestDirectory, "nul"));
        var scanner = new NulFileScanner();

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        var match = Assert.Single(result.Matches);
        Assert.True(nulPath.Length > 260);
        Assert.EndsWith("nul", match.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\\?\", match.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\\.\", match.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerTests>();
        var scanner = new NulFileScanner();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, cts.Token));
    }

    private static void WriteReservedFile(string directoryPath, string fileName, byte[] content)
    {
        Directory.CreateDirectory(ReservedPathNormalizer.Normalize(directoryPath));
        var fullPath = Path.Combine(directoryPath, fileName);
        File.WriteAllBytes(ReservedPathNormalizer.Normalize(fullPath), content);
    }

    private sealed class AccessDeniedTestScanner(string blockedDirectoryPath) : NulFileScanner
    {
        private readonly string _blockedDirectoryPath = blockedDirectoryPath;

        protected override IEnumerable<string> EnumerateFiles(string normalizedDirectoryPath)
        {
            if (PathsEqual(normalizedDirectoryPath, _blockedDirectoryPath))
            {
                throw new UnauthorizedAccessException("Access denied for test.");
            }

            return base.EnumerateFiles(normalizedDirectoryPath);
        }
    }

    private sealed class ReparsePointTestScanner(string reparseDirectoryPath) : NulFileScanner
    {
        private readonly string _reparseDirectoryPath = reparseDirectoryPath;

        protected override FileAttributes GetAttributes(string normalizedPath)
        {
            var attributes = base.GetAttributes(normalizedPath);
            if (PathsEqual(normalizedPath, _reparseDirectoryPath))
            {
                return attributes | FileAttributes.ReparsePoint;
            }

            return attributes;
        }
    }

    private sealed class DeleteTargetFilteringScanner(IReadOnlySet<string> blockedRelativePaths) : NulFileScanner
    {
        private readonly IReadOnlySet<string> _blockedRelativePaths = blockedRelativePaths;

        protected override bool IsBlockedReservedNul(NulMatch candidate)
        {
            return _blockedRelativePaths.Contains(candidate.RelativePath);
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            ReservedPathNormalizer.Normalize(left),
            ReservedPathNormalizer.Normalize(right),
            StringComparison.OrdinalIgnoreCase);
    }
}
