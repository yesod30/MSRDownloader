using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MSRDownloader.Helpers;
using MSRDownloader.ViewModels;
using MSRDownloader.Views;

namespace MSRDownloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = FileHelper.ReadOptions();
            desktop.MainWindow = new MainWindow();
            desktop.MainWindow.DataContext = new MainWindowViewModel(options);
        }

        base.OnFrameworkInitializationCompleted();
    }
}