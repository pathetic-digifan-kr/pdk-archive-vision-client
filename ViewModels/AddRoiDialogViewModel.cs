using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdkOcrClient.ViewModels
{
    public partial class AddRoiDialogViewModel : ObservableObject
    {
        private readonly Action<string?>? _closeAction;

        [ObservableProperty]
        private string? _roiName;

        public AddRoiDialogViewModel(Action<string?>? closeAction = null)
        {
            _closeAction = closeAction;
        }

        [RelayCommand]
        private void Confirm()
        {
            var roiName = string.IsNullOrWhiteSpace(RoiName) ? null : RoiName;
            _closeAction?.Invoke(roiName);
        }

        [RelayCommand]
        private void Cancel()
        {
            _closeAction?.Invoke(null);
        }
    }
}