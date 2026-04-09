using System.Windows;
using Nulmesis.Core.Domain.Models;

namespace Nulmesis.App.Views;

/// <summary>
/// 删除确认对话框
/// </summary>
public partial class DeleteConfirmationDialog : Window
{
    public DeleteConfirmationDto ConfirmationData { get; }

    public DeleteConfirmationDialog(DeleteConfirmationDto data)
    {
        InitializeComponent();
        ConfirmationData = data ?? throw new ArgumentNullException(nameof(data));
        DataContext = data;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}