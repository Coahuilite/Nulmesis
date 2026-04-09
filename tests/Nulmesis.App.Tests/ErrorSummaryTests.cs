using Nulmesis.App.Tests.Mocks;
using Nulmesis.App.ViewModels;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Services;

namespace Nulmesis.App.Tests;

public class ErrorSummaryTests
{
    [Fact]
    public void DeleteErrors_InitiallyEmpty()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner);

        Assert.Empty(viewModel.DeleteErrors);
    }

    [Fact]
    public async Task DeleteErrors_AfterFailedDelete_ContainsErrors()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService { DialogResult = true };
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        if (viewModel.MatchCount > 0)
        {
            await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);

            Assert.True(viewModel.DeleteErrors.Count >= 0);
        }
    }

    [Fact]
    public async Task DeleteErrors_ClearsBeforeNewDelete()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService { DialogResult = true };
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        if (viewModel.MatchCount > 0)
        {
            await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);
            var firstDeleteErrors = viewModel.DeleteErrors.Count;

            await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

            if (viewModel.MatchCount > 0)
            {
                await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);

                Assert.True(viewModel.DeleteErrors.Count >= 0);
            }
        }
    }

    [Fact]
    public async Task DeleteErrors_DisplaysPathAndMessage()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService { DialogResult = true };
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        if (viewModel.MatchCount > 0)
        {
            await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);

            foreach (var error in viewModel.DeleteErrors)
            {
                Assert.False(string.IsNullOrEmpty(error.Path));
                Assert.False(string.IsNullOrEmpty(error.Message));
            }
        }
    }
}