using System.Windows;
using Nulmesis.App.Views;
using Nulmesis.Core.Domain.Models;

namespace Nulmesis.App.Services;

/// <summary>
/// WPF 对话框服务实现
/// </summary>
public sealed class WpfDialogService : IDialogService
{
    private readonly Window _owner;

    public WpfDialogService(Window owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public bool ShowDeleteConfirmation(DeleteConfirmationDto data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var dialog = new DeleteConfirmationDialog(data)
        {
            Owner = _owner
        };

        var result = dialog.ShowDialog();
        return result == true;
    }
}