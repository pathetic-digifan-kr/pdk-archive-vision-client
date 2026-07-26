using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdkOcrClient.ViewModels;

public partial class RoiTemplateNameDialogViewModel : ObservableObject
{
    private readonly Action<string?>? _closeAction;

    [ObservableProperty]
    private string? _templateName;

    public RoiTemplateNameDialogViewModel(Action<string?>? closeAction = null)
    {
        _closeAction = closeAction;
    }

    [RelayCommand]
    private void Confirm()
    {
        var templateName = string.IsNullOrWhiteSpace(TemplateName) ? null : TemplateName.Trim();
        _closeAction?.Invoke(templateName);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}
