using System.Windows;
using System.Windows.Controls;
using Nulmesis.App.ViewModels;
using Nulmesis.Core.Domain.Models;

namespace Nulmesis.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetViewModel(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
    }

    private void OnMatchesSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || sender is not DataGrid dataGrid)
        {
            return;
        }

        viewModel.SetSelectedMatches(dataGrid.SelectedItems.OfType<NulMatch>());
    }
}
