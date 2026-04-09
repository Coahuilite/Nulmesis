using System.Windows;
using Nulmesis.App.Services;
using Nulmesis.App.ViewModels;
using Nulmesis.Core.Services;

namespace Nulmesis.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var scanner = new NulFileScanner();
        var deleter = new NulFileDeleter();

        var mainWindow = new MainWindow();
        var dialogService = new WpfDialogService(mainWindow);

        var viewModel = new MainWindowViewModel(scanner, deleter, dialogService);

        mainWindow.SetViewModel(viewModel);
        mainWindow.Show();

        await viewModel.InitializeAsync();
    }
}