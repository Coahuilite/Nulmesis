using Nulmesis.Core.Domain.Models;

namespace Nulmesis.Core.Domain.Events;

/// <summary>
/// Event data raised after a matched file is deleted.
/// </summary>
public sealed class NulMatchDeletedEventArgs : EventArgs
{
    public required NulMatch Match { get; init; }

    public NulMatchDeletedEventArgs() { }
}
