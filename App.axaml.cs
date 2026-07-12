using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace PdkOcrClient;

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
            var dialogService = new WindowDialogService();
            var MainWindowViewModel = new MainWindowViewModel(dialogService);
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = MainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}