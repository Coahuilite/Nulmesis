using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nulmesis.App.Services;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Services;

namespace Nulmesis.App.ViewModels;

public enum ViewModelState
{
    Idle,
    Scanning,
    ReviewReady,
    AwaitingConfirmation,
    Deleting
}

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly NulFileScanner _scanner;
    private readonly NulFileDeleter? _deleter;
    private readonly IDialogService? _dialogService;

    [ObservableProperty]
    private ViewModelState _state = ViewModelState.Idle;

    [ObservableProperty]
    private string _rootDirectory = AppContext.BaseDirectory;

    [ObservableProperty]
    private ScanMode _selectedMode = ScanMode.Strict;

    [ObservableProperty]
    private int _matchCount;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public ObservableCollection<NulMatch> Matches { get; } = new();
    public ObservableCollection<DeleteError> DeleteErrors { get; } = new();

    public IReadOnlyList<ScanMode> AvailableModes { get; } = new[] { ScanMode.Strict, ScanMode.Loose };

    public MainWindowViewModel(NulFileScanner scanner, NulFileDeleter? deleter = null, IDialogService? dialogService = null)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _deleter = deleter;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task ScanAsync(CancellationToken ct)
    {
        if (State == ViewModelState.Scanning || State == ViewModelState.Deleting)
        {
            return;
        }

        State = ViewModelState.Scanning;
        StatusMessage = "正在扫描...";
        Matches.Clear();
        DeleteErrors.Clear();
        MatchCount = 0;
        ErrorCount = 0;

        try
        {
            var result = await _scanner.ScanAsync(RootDirectory, SelectedMode, ct);

            foreach (var match in result.DeleteTargets)
            {
                Matches.Add(match);
            }

            MatchCount = result.Summary.MatchedCount;
            ErrorCount = result.Summary.ErrorCount;

            if (MatchCount == 0)
            {
                StatusMessage = "未发现可删除的 nul 文件";
                State = ViewModelState.Idle;
            }
            else if (Matches.Count == 0)
            {
                StatusMessage = $"发现 {MatchCount} 个 nul 候选，但都可通过普通路径处理，未加入删除列表";
                State = ViewModelState.Idle;
            }
            else
            {
                State = ViewModelState.ReviewReady;
                StatusMessage = $"扫描完成，找到 {MatchCount} 个匹配项，其中 {Matches.Count} 个需要通过扩展路径删除";
            }
        }
        catch (OperationCanceledException)
        {
            State = ViewModelState.Idle;
            StatusMessage = "扫描已取消";
        }
        catch (Exception ex)
        {
            State = ViewModelState.Idle;
            StatusMessage = $"扫描失败: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync(CancellationToken ct)
    {
        if (_deleter == null)
        {
            StatusMessage = "删除功能未配置";
            return;
        }

        if (Matches.Count == 0)
        {
            StatusMessage = "没有可删除的项";
            return;
        }

        State = ViewModelState.AwaitingConfirmation;

        var confirmationData = new DeleteConfirmationDto
        {
            Root = RootDirectory,
            Mode = SelectedMode,
            MatchedCount = Matches.Count,
            PreviewPaths = Matches.Take(10).Select(m => m.RelativePath).ToList()
        };

        var confirmed = _dialogService?.ShowDeleteConfirmation(confirmationData) ?? true;

        if (!confirmed)
        {
            State = ViewModelState.ReviewReady;
            StatusMessage = "删除已取消";
            return;
        }

        await ExecuteDeleteAsync(ct);
    }

    private async Task ExecuteDeleteAsync(CancellationToken ct)
    {
        State = ViewModelState.Deleting;
        StatusMessage = "正在删除...";
        DeleteErrors.Clear();

        try
        {
            if (_deleter == null)
            {
                throw new InvalidOperationException("删除器未配置");
            }

            var result = await _deleter.DeleteAsync(Matches.ToList(), ct);

            foreach (var error in result.Errors)
            {
                DeleteErrors.Add(error);
            }

            if (result.Errors.Count > 0)
            {
                StatusMessage = $"删除完成，成功 {result.Summary.DeletedCount} 个，失败 {result.Summary.FailedCount} 个";
            }
            else
            {
                StatusMessage = $"已删除 {result.Summary.DeletedCount} 个文件";
            }

            await ScanCommand.ExecuteAsync(ct);
        }
        catch (Exception ex)
        {
            State = ViewModelState.ReviewReady;
            StatusMessage = $"删除失败: {ex.Message}";
        }
    }

    private bool CanDelete()
    {
        return State == ViewModelState.ReviewReady && Matches.Count > 0;
    }

    /// <summary>
    /// 启动后自动执行首次扫描
    /// </summary>
    public async Task InitializeAsync()
    {
        await ScanCommand.ExecuteAsync(CancellationToken.None);
    }
}
