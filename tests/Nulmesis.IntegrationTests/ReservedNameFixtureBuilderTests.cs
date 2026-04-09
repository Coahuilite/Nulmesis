using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Tests.Shared;

namespace Nulmesis.IntegrationTests;

public class ReservedNameFixtureBuilderTests
{
    [Fact]
    public void ReservedNameFixtureBuilder_CreatesNul()
    {
        using var testRoot = TempTestRootFactory.Create<ReservedNameFixtureBuilderTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);

        var nulPath = fixture.CreateReadOnlyNulFile("nul");
        var extendedNulPath = ReservedPathNormalizer.Normalize(nulPath);

        Assert.True(File.Exists(extendedNulPath));
        Assert.Equal("nul", Path.GetFileName(nulPath));
        Assert.True((File.GetAttributes(extendedNulPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
        Assert.StartsWith(Path.Combine(Path.GetTempPath(), "Nulmesis.Tests"), testRoot.RootPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReservedNameFixtureBuilder_DoesNotPolluteWorkspace()
    {
        var workspaceLeakPath = Path.Combine(AppContext.BaseDirectory, "Nulmesis.Tests");
        string isolatedRoot;

        using (var testRoot = TempTestRootFactory.Create<ReservedNameFixtureBuilderTests>())
        {
            isolatedRoot = testRoot.RootPath;
            var fixture = ReservedNameFixtureBuilder.Create(testRoot);

            fixture.CreateReadOnlyNulFile(Path.Combine("samples", "nul"));

            Assert.StartsWith(Path.GetTempPath(), isolatedRoot, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(AppContext.BaseDirectory, isolatedRoot, StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(workspaceLeakPath));
        }

        Assert.False(Directory.Exists(ReservedPathNormalizer.Normalize(isolatedRoot)));
        Assert.False(Directory.Exists(workspaceLeakPath));
    }

    [Fact]
    public void ReservedNameFixtureBuilder_CreatesNulUnderLongPath()
    {
        using var testRoot = TempTestRootFactory.Create<ReservedNameFixtureBuilderTests>();
        var fixture = ReservedNameFixtureBuilder.Create(testRoot);

        var deepestDirectory = fixture.CreateNestedTree(depth: 6, filesPerLevel: 2);
        var nulPath = fixture.CreateReadOnlyNulFile(Path.Combine(deepestDirectory, "nul"));
        var extendedNulPath = ReservedPathNormalizer.Normalize(nulPath);

        Assert.True(nulPath.Length > 260, $"Expected a long path, but got length {nulPath.Length}: {nulPath}");
        Assert.True(File.Exists(extendedNulPath));
        Assert.True((File.GetAttributes(extendedNulPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
    }
}
