using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdkOcrClient.Services;

namespace PdkOcrClient.ViewModels;

public partial class RoiTemplateLoadDialogViewModel : ObservableObject
{
    private readonly Action<RoiTemplate?> _closeAction;

    [ObservableProperty]
    private RoiTemplateListItem? _selectedTemplate;

    public ObservableCollection<RoiTemplateListItem> Templates { get; }

    public RoiTemplateLoadDialogViewModel(
        ObservableCollection<RoiTemplateListItem> templates,
        Action<RoiTemplate?> closeAction)
    {
        Templates = templates;
        _closeAction = closeAction;
        SelectedTemplate = Templates.Count > 0 ? Templates[0] : null;
    }

    [RelayCommand]
    private void Load()
    {
        _closeAction(SelectedTemplate?.Template);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction(null);
    }
}

public sealed class RoiTemplateListItem : IDisposable
{
    public RoiTemplate Template { get; }
    public string Name => Template.Name;
    public string CreatedAtText => Template.CreatedAt.ToString("yyyy-MM-dd HH:mm");
    public string RegionCountText => $"ROI {Template.Regions.Count}";
    public string ImageStatusText => ImagePreview is null ? "저장된 이미지 없음" : Template.TargetImageFileName ?? string.Empty;
    public Bitmap? ImagePreview { get; }

    public RoiTemplateListItem(RoiTemplate template, string? imagePath)
    {
        Template = template;

        if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
        {
            using var stream = File.OpenRead(imagePath);
            ImagePreview = new Bitmap(stream);
        }
    }

    public void Dispose()
    {
        ImagePreview?.Dispose();
    }
}
