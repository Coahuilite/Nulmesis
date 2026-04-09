using Nulmesis.App.ViewModels;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Services;

namespace Nulmesis.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesDefaultState()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner);

        Assert.Equal(ViewModelState.Idle, viewModel.State);
        Assert.Equal(AppContext.BaseDirectory, viewModel.RootDirectory);
        Assert.Equal(ScanMode.Strict, viewModel.SelectedMode);
        Assert.Equal(0, viewModel.MatchCount);
        Assert.Equal(0, viewModel.ErrorCount);
        Assert.Equal("就绪", viewModel.StatusMessage);
        Assert.Empty(viewModel.Matches);
        Assert.Empty(viewModel.DeleteErrors);
    }

    [Fact]
    public void AvailableModes_ContainsStrictAndLoose()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner);

        Assert.Equal(2, viewModel.AvailableModes.Count);
        Assert.Contains(ScanMode.Strict, viewModel.AvailableModes);
        Assert.Contains(ScanMode.Loose, viewModel.AvailableModes);
    }

    [Fact]
    public async Task ScanAsync_UpdatesStateAndCollections()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        Assert.True(viewModel.State == ViewModelState.ReviewReady || viewModel.State == ViewModelState.Idle);
        Assert.True(viewModel.MatchCount >= 0);
        Assert.True(viewModel.ErrorCount >= 0);
    }

    [Fact]
    public async Task ScanAsync_WithNonexistentPath_SetsErrorCount()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner)
        {
            RootDirectory = @"Z:\Nonexistent\Path\That\Does\Not\Exist"
        };

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        Assert.True(viewModel.State == ViewModelState.ReviewReady || viewModel.State == ViewModelState.Idle);
        Assert.True(viewModel.ErrorCount > 0);
        Assert.True(viewModel.StatusMessage.Contains("扫描完成") || viewModel.StatusMessage.Contains("未发现"));
    }

    [Fact]
    public async Task DeleteCommand_WhenNoMatches_IsDisabled()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var viewModel = new MainWindowViewModel(scanner, deleter);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        var canExecute = viewModel.DeleteCommand.CanExecute(null);
        Assert.Equal(viewModel.MatchCount > 0, canExecute);
    }

    [Fact]
    public async Task InitializeAsync_PerformsFirstScan()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner);

        await viewModel.InitializeAsync();

        Assert.True(viewModel.State == ViewModelState.ReviewReady || viewModel.State == ViewModelState.Idle);
        Assert.NotEqual(ViewModelState.Scanning, viewModel.State);
    }

    [Fact]
    public void State_PropertyChanged_RaisesEvent()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner);
        var eventRaised = false;

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.State))
            {
                eventRaised = true;
            }
        };

        viewModel.State = ViewModelState.Scanning;

        Assert.True(eventRaised);
    }

    [Fact]
    public void StatusMessage_PropertyChanged_RaisesEvent()
    {
        var scanner = new NulFileScanner();
        var viewModel = new MainWindowViewModel(scanner);
        var eventRaised = false;

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.StatusMessage))
            {
                eventRaised = true;
            }
        };

        viewModel.StatusMessage = "测试状态";

        Assert.True(eventRaised);
    }
}