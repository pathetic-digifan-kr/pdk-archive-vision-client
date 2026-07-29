using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PdkOcrClient.ViewModels;

namespace PdkOcrClient.Services;

public class WindowDialogService : IDialogService
{
    private readonly RoiTemplateStorageService _roiTemplateStorageService = new();

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

    public async Task<RoiTemplate?> OpenRoiTemplateLoadDialogAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is Window mainWindow)
        {
            var templates = await _roiTemplateStorageService.LoadAllTemplatesAsync();
            var items = new ObservableCollection<RoiTemplateListItem>(
                templates
                    .OrderByDescending(template => template.CreatedAt)
                    .Select(template => new RoiTemplateListItem(
                        template,
                        _roiTemplateStorageService.GetTemplateImagePath(template))));

            var dialog = new PdkOcrClient.Dialog.RoiTemplateLoadDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = new RoiTemplateLoadDialogViewModel(items, result =>
            {
                dialog.Close(result);
            });

            dialog.DataContext = viewModel;

            try
            {
                return await dialog.ShowDialog<RoiTemplate?>(mainWindow);
            }
            finally
            {
                foreach (var item in items)
                {
                    item.Dispose();
                }
            }
        }

        return null;
    }
}
