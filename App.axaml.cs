using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PdkOcrClient.Services;

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
            var ocrClient = new OcrClient();
            var MainWindowViewModel = new MainWindowViewModel(dialogService, ocrClient);
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = MainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}