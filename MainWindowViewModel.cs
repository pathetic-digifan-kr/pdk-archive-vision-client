using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdkOcrClient;
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private Bitmap? _mainImage;

    [ObservableProperty]
    private string _selectedFileName = string.Empty;

    private readonly IDialogService _dialogService;

    public MainWindowViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task OpenFileDialogAsync()
    {
        var filePath = await _dialogService.OpenFileDialogAsync("Select an image file");
        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                // 💡 파일 스트림을 열어서 아발로니아 비트맵 인프라로 광속 덤프 변환!!!
                using var stream = File.OpenRead(filePath);
                // 기존 비트맵 메모리가 스케줄러에 남아있다면 청정하게 셧다운(Dispose) 락!
                MainImage?.Dispose();

                // 새 이미지 비트맵 최종 세이브 마운트!!!
                MainImage = new Bitmap(stream);
                SelectedFileName = Path.GetFileName(filePath);
            }
            catch (Exception ex)
            {
                // 이미지 로드 실패 시 안전 방화벽 예외 처리 노이즈 차단
                System.Diagnostics.Debug.WriteLine($"이미지 로드 실패: {ex.Message}");
            }        
        }
    }
}