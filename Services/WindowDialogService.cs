using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PdkOcrClient.ViewModels;

public class WindowDialogService : IDialogService
{
    public async Task<string?> OpenFileDialogAsync(string title)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is Window mainWindow)
        {
            var storageProvider = TopLevel.GetTopLevel(mainWindow)?.StorageProvider;
            if (storageProvider == null) return null;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });

            if (files.Any())
            {
                return files[0].Path.LocalPath;
            }
        }
        return null;
    }

    public async Task<string?> OpenAddRoiDialogAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is Window mainWindow)
        {
            var dialog = new PdkOcrClient.Dialog.AddRoiDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = new AddRoiDialogViewModel(result =>
            {
                dialog.Close(result);
            });

            dialog.DataContext = viewModel;

            return await dialog.ShowDialog<string?>(mainWindow);
        }

        return null;
    }

    public async Task<string?> OpenRoiTemplateNameDialogAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is Window mainWindow)
        {
            var dialog = new PdkOcrClient.Dialog.RoiTemplateNameDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = new RoiTemplateNameDialogViewModel(result =>
            {
                dialog.Close(result);
            });

            dialog.DataContext = viewModel;

            return await dialog.ShowDialog<string?>(mainWindow);
        }

        return null;
    }
}
