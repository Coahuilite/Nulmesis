using System.Diagnostics;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Services;
using Nulmesis.Tests.Shared;

namespace Nulmesis.IntegrationTests;

public sealed class PublishSmokePerformanceTests
{
    [Fact]
    [Trait("Category", "PublishSmoke")]
    public async Task PublishSmoke_Scan_Strict_CompletesWithinThirtySeconds_OnLargeTree()
    {
        using var testRoot = TempTestRootFactory.Create<PublishSmokePerformanceTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var deepestDirectory = fixture.CreateNestedTree(depth: 50, filesPerLevel: 200);
        var nulPath = fixture.CreateReadOnlyNulFile(Path.Combine(deepestDirectory, "nul"));
        var scanner = new NulFileScanner();
        var stopwatch = Stopwatch.StartNew();

        var result = await scanner.ScanAsync(testRoot.RootPath, ScanMode.Strict, CancellationToken.None);

        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Strict scan exceeded 30 seconds: {stopwatch.Elapsed}.");
        Assert.True(result.Summary.DurationMs < 30_000, $"Scanner reported duration {result.Summary.DurationMs} ms.");
        Assert.Contains(result.Matches, match => string.Equals(match.AbsolutePath, nulPath, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, result.Summary.MatchedCount);
        Assert.Empty(result.Errors);
    }
}
