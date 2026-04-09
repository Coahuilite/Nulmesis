using System.Diagnostics;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Core.Domain.Policies;

namespace Nulmesis.Core.Services;

/// <summary>
/// Recursively scans a directory tree for files matching the Windows reserved name "nul".
/// </summary>
public class NulFileScanner
{
    private static readonly EnumerationOptions DirectoryEnumerationOptions = new()
    {
        AttributesToSkip = 0,
        IgnoreInaccessible = false,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false
    };

    private static readonly EnumerationOptions FileEnumerationOptions = new()
    {
        AttributesToSkip = 0,
        IgnoreInaccessible = false,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false
    };

    public async Task<ScanResult> ScanAsync(string rootPath, ScanMode mode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        ct.ThrowIfCancellationRequested();
        await Task.Yield();

        var stopwatch = Stopwatch.StartNew();
        var fullRootPath = Path.GetFullPath(rootPath);
        var normalizedRootPath = ReservedPathNormalizer.Normalize(fullRootPath);
        var matches = new List<NulMatch>();
        var deleteTargets = new List<NulMatch>();
        var errors = new List<ScanError>();

        if (!DirectoryExists(normalizedRootPath))
        {
            errors.Add(new ScanError
            {
                Kind = ScanErrorKind.DirectoryNotFound,
                Path = fullRootPath,
                Message = $"Root directory '{fullRootPath}' was not found."
            });

            return BuildResult(fullRootPath, mode, matches, deleteTargets, errors, stopwatch.ElapsedMilliseconds);
        }

        ScanDirectory(fullRootPath, normalizedRootPath, fullRootPath, mode, matches, deleteTargets, errors, ct);
        return BuildResult(fullRootPath, mode, matches, deleteTargets, errors, stopwatch.ElapsedMilliseconds);
    }

    protected virtual IEnumerable<string> EnumerateDirectories(string normalizedDirectoryPath)
        => Directory.EnumerateDirectories(normalizedDirectoryPath, "*", DirectoryEnumerationOptions);

    protected virtual IEnumerable<string> EnumerateFiles(string normalizedDirectoryPath)
        => Directory.EnumerateFiles(normalizedDirectoryPath, "*", FileEnumerationOptions);

    protected virtual FileAttributes GetAttributes(string normalizedPath)
        => File.GetAttributes(normalizedPath);

    protected virtual bool DirectoryExists(string normalizedPath)
        => Directory.Exists(normalizedPath);

    protected virtual (long SizeBytes, DateTime LastWriteTimeUtc) GetFileMetadata(string normalizedFilePath)
    {
        var fileInfo = new FileInfo(normalizedFilePath);
        return (fileInfo.Length, fileInfo.LastWriteTimeUtc);
    }

    protected virtual bool IsBlockedReservedNul(NulMatch candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        try
        {
            var fileInfo = new FileInfo(candidate.AbsolutePath);
            if (!fileInfo.Exists)
            {
                return true;
            }

            return fileInfo.Length != candidate.SizeBytes
                || fileInfo.LastWriteTimeUtc != candidate.LastWriteTimeUtc;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or ArgumentException or NotSupportedException)
        {
            return true;
        }
    }

    private void ScanDirectory(
        string rootPath,
        string normalizedDirectoryPath,
        string displayDirectoryPath,
        ScanMode mode,
        List<NulMatch> matches,
        List<NulMatch> deleteTargets,
        List<ScanError> errors,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IEnumerable<string> childDirectories;
        try
        {
            childDirectories = EnumerateDirectories(normalizedDirectoryPath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            errors.Add(CreateDirectoryError(displayDirectoryPath, ex));
            return;
        }

        foreach (var childDirectory in childDirectories)
        {
            ct.ThrowIfCancellationRequested();

            var displayChildDirectory = Path.Combine(displayDirectoryPath, GetChildName(childDirectory));
            FileAttributes attributes;

            try
            {
                attributes = GetAttributes(childDirectory);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                errors.Add(CreateDirectoryError(displayChildDirectory, ex));
                continue;
            }

            if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                errors.Add(new ScanError
                {
                    Kind = ScanErrorKind.ReparsePointSkipped,
                    Path = displayChildDirectory,
                    Message = $"Skipped reparse point directory '{displayChildDirectory}'."
                });
                continue;
            }

            ScanDirectory(rootPath, childDirectory, displayChildDirectory, mode, matches, deleteTargets, errors, ct);
        }

        IEnumerable<string> childFiles;
        try
        {
            childFiles = EnumerateFiles(normalizedDirectoryPath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            errors.Add(CreateDirectoryError(displayDirectoryPath, ex));
            return;
        }

        foreach (var childFile in childFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fileName = GetChildName(childFile);
            var displayFilePath = Path.Combine(displayDirectoryPath, fileName);

            if (NulMatchPolicy.IsDisqualified(fileName))
            {
                continue;
            }

            (long SizeBytes, DateTime LastWriteTimeUtc) metadata;
            try
            {
                metadata = GetFileMetadata(childFile);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                errors.Add(new ScanError
                {
                    Kind = ex is UnauthorizedAccessException ? ScanErrorKind.AccessDenied : ScanErrorKind.IoFailure,
                    Path = displayFilePath,
                    Message = ex.Message,
                    Exception = ex
                });
                continue;
            }

            var candidate = new NulMatch
            {
                AbsolutePath = displayFilePath,
                RelativePath = BuildRelativePath(rootPath, displayDirectoryPath, fileName),
                FileName = fileName,
                SizeBytes = metadata.SizeBytes,
                LastWriteTimeUtc = metadata.LastWriteTimeUtc
            };

            if (NulMatchPolicy.IsMatch(candidate, mode))
            {
                matches.Add(candidate);

                if (IsBlockedReservedNul(candidate))
                {
                    deleteTargets.Add(candidate);
                }
            }
        }
    }

    private static ScanResult BuildResult(
        string rootPath,
        ScanMode mode,
        List<NulMatch> matches,
        List<NulMatch> deleteTargets,
        List<ScanError> errors,
        long durationMs)
    {
        return new ScanResult
        {
            Matches = matches,
            DeleteTargets = deleteTargets,
            Errors = errors,
            Summary = new ScanSummary
            {
                Root = rootPath,
                Mode = mode,
                MatchedCount = matches.Count,
                ErrorCount = errors.Count,
                DurationMs = durationMs
            }
        };
    }

    private static ScanError CreateDirectoryError(string displayPath, Exception ex)
    {
        return new ScanError
        {
            Kind = ex is UnauthorizedAccessException ? ScanErrorKind.AccessDenied : ScanErrorKind.IoFailure,
            Path = displayPath,
            Message = ex.Message,
            Exception = ex
        };
    }

    private static string ToDisplayPath(string path)
    {
        const string extendedPrefix = @"\\?\";
        const string uncExtendedPrefix = @"\\?\UNC\";

        if (path.StartsWith(uncExtendedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return @"\\" + path[uncExtendedPrefix.Length..];
        }

        if (path.StartsWith(extendedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[extendedPrefix.Length..];
        }

        return path;
    }

    private static string GetChildName(string enumeratedPath)
    {
        var normalizedDisplayPath = ToDisplayPath(enumeratedPath);
        var childName = Path.GetFileName(normalizedDisplayPath);

        if (string.IsNullOrWhiteSpace(childName))
        {
            var trimmedPath = normalizedDisplayPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var lastSeparatorIndex = trimmedPath.LastIndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
            if (lastSeparatorIndex >= 0 && lastSeparatorIndex < trimmedPath.Length - 1)
            {
                childName = trimmedPath[(lastSeparatorIndex + 1)..];
            }
        }

        if (string.IsNullOrWhiteSpace(childName))
        {
            throw new InvalidOperationException($"Unable to determine child name from path '{enumeratedPath}'.");
        }

        return childName;
    }

    private static string BuildRelativePath(string rootPath, string displayDirectoryPath, string fileName)
    {
        if (string.Equals(rootPath, displayDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }

        return Path.Combine(Path.GetRelativePath(rootPath, displayDirectoryPath), fileName);
    }
}
