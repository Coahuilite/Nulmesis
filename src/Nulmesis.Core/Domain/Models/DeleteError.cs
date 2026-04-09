namespace Nulmesis.Core.Domain.Models;

/// <summary>
/// Error encountered during a delete operation.
/// </summary>
public sealed class DeleteError
{
    public required string Path { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }

    public DeleteError() { }
}
