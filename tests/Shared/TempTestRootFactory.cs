using Nulmesis.Core.Domain.Normalizers;

namespace Nulmesis.Tests.Shared;

public sealed class TempTestRootFactory : IDisposable
{
    private const int DeleteRetryCount = 5;
    private static readonly TimeSpan DeleteRetryDelay = TimeSpan.FromMilliseconds(50);
    private bool _disposed;

    public TempTestRootFactory(string testClassName)
    {
        if (string.IsNullOrWhiteSpace(testClassName))
        {
            throw new ArgumentException("Test class name is required.", nameof(testClassName));
        }

        TestClassName = SanitizeSegment(testClassName);
        RootPath = Path.Combine(Path.GetTempPath(), "Nulmesis.Tests", TestClassName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ExtendedRootPath);
    }

    public string TestClassName { get; }

    public string RootPath { get; }

    public string ExtendedRootPath => ReservedPathNormalizer.Normalize(RootPath);

    public static TempTestRootFactory Create<TTestClass>() => new(typeof(TTestClass).Name);

    public static TempTestRootFactory Create(string testClassName) => new(testClassName);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ForceDeleteDirectory(RootPath);
        GC.SuppressFinalize(this);
    }

    private static void ForceDeleteDirectory(string path)
    {
        var extendedPath = ReservedPathNormalizer.Normalize(path);
        if (!Directory.Exists(extendedPath))
        {
            return;
        }

        Exception? lastException = null;

        for (var attempt = 0; attempt < DeleteRetryCount; attempt++)
        {
            try
            {
                DeleteDirectoryContents(extendedPath);
                Directory.Delete(extendedPath, recursive: false);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lastException = ex;
                Thread.Sleep(DeleteRetryDelay);
            }
        }

        throw new IOException($"Failed to delete test root '{path}'.", lastException);
    }

    private static void DeleteDirectoryContents(string extendedDirectoryPath)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(extendedDirectoryPath))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                DeleteDirectoryContents(entry);
                Directory.Delete(entry, recursive: false);
                continue;
            }

            File.SetAttributes(entry, FileAttributes.Normal);
            File.Delete(entry);
        }
    }

    private static string SanitizeSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var buffer = value.ToCharArray();

        for (var index = 0; index < buffer.Length; index++)
        {
            if (invalidCharacters.Contains(buffer[index]))
            {
                buffer[index] = '_';
            }
        }

        return new string(buffer);
    }
}
