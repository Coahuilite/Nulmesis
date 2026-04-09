using Nulmesis.App.Services;
using Nulmesis.Core.Domain.Models;

namespace Nulmesis.App.Tests.Mocks;

public sealed class MockDialogService : IDialogService
{
    public bool DialogResult { get; set; } = true;
    public DeleteConfirmationDto? LastConfirmationData { get; private set; }
    public int ShowConfirmationCallCount { get; private set; }

    public bool ShowDeleteConfirmation(DeleteConfirmationDto data)
    {
        LastConfirmationData = data;
        ShowConfirmationCallCount++;
        return DialogResult;
    }
}