using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Core.Services;
using Nulmesis.Tests.Shared;

namespace Nulmesis.IntegrationTests;

public sealed class NulFileDeleterTests
{
    [Fact]
    public async Task NulFileDeleter_DeletesReservedName()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileDeleterTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var nulPath = fixture.CreateRegularFile("nul", string.Empty);
        var sut = new NulFileDeleter();
        NulMatch? deletedMatch = null;

        sut.Deleted += (_, args) => deletedMatch = args.Match;

        var result = await sut.DeleteAsync([CreateMatch(testRoot.RootPath, nulPath)], CancellationToken.None);

        Assert.Equal(1, result.Summary.RequestedCount);
        Assert.Equal(1, result.Summary.DeletedCount);
        Assert.Equal(0, result.Summary.FailedCount);
        Assert.False(result.Summary.Cancelled);
        Assert.Empty(result.Errors);
        Assert.False(File.Exists(ReservedPathNormalizer.Normalize(nulPath)));
        Assert.NotNull(deletedMatch);
        Assert.Equal(nulPath, deletedMatch.AbsolutePath);
    }

    [Fact]
    public async Task NulFileDeleter_ReportsPartialFailure()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileDeleterTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var deletablePath = fixture.CreateRegularFile(Path.Combine("ok", "nul"), string.Empty);
        var externallyDeletedPath = fixture.CreateRegularFile(Path.Combine("missing", "nul"), string.Empty);
        var externallyDeletedExtendedPath = ReservedPathNormalizer.Normalize(externallyDeletedPath);
        var sut = new NulFileDeleter();

        File.Delete(externallyDeletedExtendedPath);

        var result = await sut.DeleteAsync(
        [
            CreateMatch(testRoot.RootPath, deletablePath),
            CreateMatch(testRoot.RootPath, externallyDeletedPath)
        ],
        CancellationToken.None);

        Assert.Equal(2, result.Summary.RequestedCount);
        Assert.Equal(1, result.Summary.DeletedCount);
        Assert.Equal(1, result.Summary.FailedCount);
        Assert.Single(result.Errors);
        Assert.Equal(externallyDeletedPath, result.Errors[0].Path);
        Assert.False(File.Exists(ReservedPathNormalizer.Normalize(deletablePath)));
    }

    [Fact]
    public async Task NulFileDeleter_NoOpForEmptyTargets()
    {
        var sut = new NulFileDeleter();

        var result = await sut.DeleteAsync([], CancellationToken.None);

        Assert.Equal(0, result.Summary.RequestedCount);
        Assert.Equal(0, result.Summary.DeletedCount);
        Assert.Equal(0, result.Summary.FailedCount);
        Assert.False(result.Summary.Cancelled);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task NulFileDeleter_DeletesReadOnlyFile()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileDeleterTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var nulPath = fixture.CreateReadOnlyNulFile(Path.Combine("readonly", "nul"));
        var extendedPath = ReservedPathNormalizer.Normalize(nulPath);
        var sut = new NulFileDeleter();

        var result = await sut.DeleteAsync([CreateMatch(testRoot.RootPath, nulPath)], CancellationToken.None);

        Assert.Equal(1, result.Summary.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(File.Exists(extendedPath));
    }

    [Fact]
    public async Task NulFileDeleter_RecordsMissingFileWithoutThrowing()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileDeleterTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var nulPath = fixture.CreateRegularFile(Path.Combine("gone", "nul"), string.Empty);
        var sut = new NulFileDeleter();

        File.Delete(ReservedPathNormalizer.Normalize(nulPath));

        var result = await sut.DeleteAsync([CreateMatch(testRoot.RootPath, nulPath)], CancellationToken.None);

        Assert.Equal(0, result.Summary.DeletedCount);
        Assert.Equal(1, result.Summary.FailedCount);
        Assert.Single(result.Errors);
        Assert.Equal(nulPath, result.Errors[0].Path);
        Assert.NotNull(result.Errors[0].Exception);
    }

    [Fact]
    public async Task NulFileDeleter_DeletesLongPathFile()
    {
        using var testRoot = TempTestRootFactory.Create<NulFileDeleterTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);
        var deepestDirectory = fixture.CreateNestedTree(depth: 6, filesPerLevel: 1);
        var nulPath = fixture.CreateRegularFile(Path.Combine(deepestDirectory, "nul"), string.Empty);
        var sut = new NulFileDeleter();

        var result = await sut.DeleteAsync([CreateMatch(testRoot.RootPath, nulPath)], CancellationToken.None);

        Assert.True(nulPath.Length > 260, $"Expected a long path, but got length {nulPath.Length}: {nulPath}");
        Assert.Equal(1, result.Summary.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(File.Exists(ReservedPathNormalizer.Normalize(nulPath)));
    }

    private static NulMatch CreateMatch(string rootPath, string absolutePath)
    {
        var exists = File.Exists(ReservedPathNormalizer.Normalize(absolutePath));
        var fileInfo = exists ? new FileInfo(ReservedPathNormalizer.Normalize(absolutePath)) : null;

        return new NulMatch
        {
            AbsolutePath = absolutePath,
            RelativePath = Path.GetRelativePath(rootPath, absolutePath),
            FileName = Path.GetFileName(absolutePath),
            SizeBytes = fileInfo?.Length ?? 0,
            LastWriteTimeUtc = fileInfo?.LastWriteTimeUtc ?? DateTime.UnixEpoch
        };
    }
}
