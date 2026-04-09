using Nulmesis.Core.Domain.Events;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;

namespace Nulmesis.Core.Services;

/// <summary>
/// Deletes reserved-name file targets using Windows extended-length paths.
/// </summary>
public sealed class NulFileDeleter
{
    public event EventHandler<NulMatchDeletedEventArgs>? Deleted;

    public async Task<DeleteResult> DeleteAsync(List<NulMatch> targets, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(targets);

        await Task.Yield();

        var errors = new List<DeleteError>();
        var deletedCount = 0;
        var cancelled = false;

        foreach (var target in targets)
        {
            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            try
            {
                DeleteCore(target);
                deletedCount++;
                OnDeleted(target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                errors.Add(new DeleteError
                {
                    Path = target.AbsolutePath,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        return new DeleteResult
        {
            Summary = new DeleteSummary
            {
                RequestedCount = targets.Count,
                DeletedCount = deletedCount,
                FailedCount = errors.Count,
                Cancelled = cancelled
            },
            Errors = errors
        };
    }

    private static void DeleteCore(NulMatch target)
    {
        var extendedPath = ReservedPathNormalizer.Normalize(target.AbsolutePath);
        var attributes = File.GetAttributes(extendedPath);

        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            File.SetAttributes(extendedPath, attributes & ~FileAttributes.ReadOnly);
        }

        File.Delete(extendedPath);
    }

    private void OnDeleted(NulMatch target)
    {
        Deleted?.Invoke(this, new NulMatchDeletedEventArgs
        {
            Match = target
        });
    }
}
