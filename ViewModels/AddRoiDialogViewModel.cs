using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PdkOcrClient.ViewModels
{
    public class AddRoiDialogViewModel : INotifyPropertyChanged
    {
        private readonly Action<string?>? _closeAction;
        private string? _roiName;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? RoiName
        {
            get => _roiName;
            set
            {
                if (_roiName != value)
                {
                    _roiName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public AddRoiDialogViewModel(Action<string?>? closeAction = null)
        {
            _closeAction = closeAction;

            ConfirmCommand = new RelayCommand(() =>
            {
                var roiName = string.IsNullOrWhiteSpace(RoiName) ? null : RoiName;
                _closeAction?.Invoke(roiName);
            });

            CancelCommand = new RelayCommand(() =>
            {
                _closeAction?.Invoke(null);
            });
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private sealed class RelayCommand : ICommand
        {
            private readonly Action _execute;

            public RelayCommand(Action execute)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _execute();
        }
    }
}