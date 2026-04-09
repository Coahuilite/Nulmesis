using System.Text;
using Nulmesis.Core.Domain.Normalizers;

namespace Nulmesis.Tests.Shared;

public sealed class ReservedNameFixtureBuilder
{
    public ReservedNameFixtureBuilder(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(ReservedPathNormalizer.Normalize(RootPath));
    }

    public string RootPath { get; }

    public static ReservedNameFixtureBuilder Create(TempTestRootFactory rootFactory) => new(rootFactory.RootPath);

    public string CreateRegularFile(string path, string content)
    {
        var fullPath = GetFullPath(path);
        CreateParentDirectory(fullPath);
        File.WriteAllBytes(ReservedPathNormalizer.Normalize(fullPath), Encoding.UTF8.GetBytes(content));
        return fullPath;
    }

    public string CreateNestedTree(int depth, int filesPerLevel)
    {
        if (depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth must be greater than zero.");
        }

        if (filesPerLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(filesPerLevel), filesPerLevel, "Files per level cannot be negative.");
        }

        var currentPath = RootPath;

        for (var level = 1; level <= depth; level++)
        {
            currentPath = Path.Combine(currentPath, CreateLongDirectorySegment(level));
            Directory.CreateDirectory(ReservedPathNormalizer.Normalize(currentPath));

            for (var fileIndex = 1; fileIndex <= filesPerLevel; fileIndex++)
            {
                CreateRegularFile(Path.Combine(currentPath, $"sample-{level:D2}-{fileIndex:D2}.txt"), $"level={level};file={fileIndex}");
            }
        }

        return currentPath;
    }

    public string CreateReadOnlyNulFile(string path)
    {
        var fullPath = GetFullPath(path);
        CreateParentDirectory(fullPath);

        var extendedPath = ReservedPathNormalizer.Normalize(fullPath);
        File.WriteAllBytes(extendedPath, []);
        File.SetAttributes(extendedPath, FileAttributes.ReadOnly);
        return fullPath;
    }

    private void CreateParentDirectory(string fullPath)
    {
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        Directory.CreateDirectory(ReservedPathNormalizer.Normalize(directoryPath));
    }

    private string GetFullPath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(RootPath, path);
    }

    private static string CreateLongDirectorySegment(int level)
    {
        return $"level-{level:D2}-{new string((char)('a' + ((level - 1) % 26)), 32)}";
    }
}
