using Nulmesis.App.Tests.Mocks;
using Nulmesis.App.ViewModels;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Core.Services;
using Nulmesis.Tests.Shared;

namespace Nulmesis.App.Tests;

public class DeleteFlowTests
{
    [Fact]
    public async Task DeleteCommand_WithNoMatches_ShowsNoFilesMessage()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService();
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, viewModel.MatchCount);
        Assert.Contains("未发现 nul 文件", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DeleteCommand_WithMatches_ShowsConfirmationDialog()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService { DialogResult = false };
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService)
        {
            RootDirectory = AppContext.BaseDirectory
        };

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        if (viewModel.MatchCount > 0)
        {
            await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);

            Assert.Equal(1, dialogService.ShowConfirmationCallCount);
            Assert.NotNull(dialogService.LastConfirmationData);
            Assert.Equal(viewModel.RootDirectory, dialogService.LastConfirmationData.Root);
            Assert.Equal(viewModel.SelectedMode, dialogService.LastConfirmationData.Mode);
            Assert.Equal(viewModel.MatchCount, dialogService.LastConfirmationData.MatchedCount);
        }
    }

    [Fact]
    public async Task DeleteCommand_UserCancels_ReturnsToReviewReady()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService { DialogResult = false };
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        if (viewModel.MatchCount > 0)
        {
            await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);

            Assert.Equal(ViewModelState.ReviewReady, viewModel.State);
            Assert.Contains("删除已取消", viewModel.StatusMessage);
        }
    }

    [Fact]
    public async Task DeleteCommand_UserConfirms_ExecutesDelete()
    {
        using var root = TempTestRootFactory.Create<DeleteFlowTests>();
        var fixture = ReservedNameFixtureBuilder.Create(root);
        var firstTarget = fixture.CreateReadOnlyNulFile(@"delete-me\nul");
        var secondTarget = fixture.CreateReadOnlyNulFile(@"keep-me\nul");

        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService { DialogResult = true };
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService)
        {
            RootDirectory = root.RootPath
        };

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);
        var targetToDelete = viewModel.Matches.Single(match => string.Equals(match.AbsolutePath, firstTarget, StringComparison.OrdinalIgnoreCase));
        viewModel.SetSelectedMatches([targetToDelete]);

        await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, dialogService.ShowConfirmationCallCount);
        Assert.NotNull(dialogService.LastConfirmationData);
        Assert.Equal(1, dialogService.LastConfirmationData!.MatchedCount);
        Assert.Single(dialogService.LastConfirmationData.PreviewPaths);
        Assert.False(File.Exists(ReservedPathNormalizer.Normalize(firstTarget)));
        Assert.True(File.Exists(ReservedPathNormalizer.Normalize(secondTarget)));
    }

    [Fact]
    public async Task DeleteCommand_AfterDelete_RefreshesScan()
    {
        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();
        var dialogService = new MockDialogService { DialogResult = true };
        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService);

        await viewModel.ScanCommand.ExecuteAsync(CancellationToken.None);

        if (viewModel.MatchCount > 0)
        {
            await viewModel.DeleteCommand.ExecuteAsync(CancellationToken.None);

            Assert.DoesNotContain("正在删除", viewModel.StatusMessage);
        }
    }

    private static void WriteReservedFile(string directoryPath, string fileName, byte[] content)
    {
        Directory.CreateDirectory(Nulmesis.Core.Domain.Normalizers.ReservedPathNormalizer.Normalize(directoryPath));
        var fullPath = Path.Combine(directoryPath, fileName);
        File.WriteAllBytes(Nulmesis.Core.Domain.Normalizers.ReservedPathNormalizer.Normalize(fullPath), content);
    }

    private sealed class DeleteTargetFilteringScanner(IReadOnlySet<string> blockedRelativePaths) : NulFileScanner
    {
        private readonly IReadOnlySet<string> _blockedRelativePaths = blockedRelativePaths;

        protected override bool IsBlockedReservedNul(NulMatch candidate)
        {
            return _blockedRelativePaths.Contains(candidate.RelativePath);
        }
    }
}
