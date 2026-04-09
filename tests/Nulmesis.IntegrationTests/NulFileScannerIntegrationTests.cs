using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Core.Services;
using Nulmesis.Tests.Shared;

namespace Nulmesis.IntegrationTests;

public class NulFileScannerIntegrationTests
{
    [Fact]
    public async Task Scanner_ReportsErrors_WhenDirectoryAccessIsDenied()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileScannerIntegrationTests>();
        var blockedDirectory = Path.Combine(testRoot.RootPath, "blocked");
        var okDirectory = Path.Combine(testRoot.RootPath, "ok");

        Directory.CreateDirectory(ReservedPathNormalizer.Normalize(blockedDirectory));
        Directory.CreateDirectory(ReservedPathNormalizer.Normalize(okDirectory));
        File.WriteAllBytes(ReservedPathNormalizer.Normalize(Path.Combine(okDirectory, "nul")), []);

        var scanner = new AccessDeniedIntegrationScanner(blockedDirectory);

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        Assert.Single(result.Matches);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ScanErrorKind.AccessDenied, error.Kind);
        Assert.Equal(blockedDirectory, error.Path);
        Assert.Equal(1, result.Summary.ErrorCount);
    }

    private sealed class AccessDeniedIntegrationScanner(string blockedDirectoryPath) : NulFileScanner
    {
        private readonly string _blockedDirectoryPath = blockedDirectoryPath;

        protected override IEnumerable<string> EnumerateFiles(string normalizedDirectoryPath)
        {
            if (string.Equals(
                ReservedPathNormalizer.Normalize(normalizedDirectoryPath),
                ReservedPathNormalizer.Normalize(_blockedDirectoryPath),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Access denied for integration test.");
            }

            return base.EnumerateFiles(normalizedDirectoryPath);
        }
    }
}
