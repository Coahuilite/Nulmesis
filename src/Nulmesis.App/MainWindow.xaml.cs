using System.Windows;
using Nulmesis.App.ViewModels;

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
}