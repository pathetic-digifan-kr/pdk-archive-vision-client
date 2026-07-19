using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

    [ObservableProperty]
    private ObservableCollection<InspectionRegion> _inspectionRegions = [];

    [ObservableProperty]
    private double _currentRoiX;

    [ObservableProperty]
    private double _currentRoiY;

    [ObservableProperty]
    private double _currentRoiWidth;

    [ObservableProperty]
    private double _currentRoiHeight;

    [ObservableProperty]
    private bool _currentRoiIsVisible;

    [ObservableProperty]
    private double _currentRoiCanvasWidth = 1;

    [ObservableProperty]
    private double _currentRoiCanvasHeight = 1;

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
                using var stream = File.OpenRead(filePath);
                MainImage?.Dispose();

                MainImage = new Bitmap(stream);
                SelectedFileName = Path.GetFileName(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"이미지 로드 실패: {ex.Message}");
            }
        }
    }

    public void UpdateCurrentRoiState(double x, double y, double width, double height, bool isVisible)
    {
        if(MainImage is null)
        {
            return;
        }

        CurrentRoiX = x;
        CurrentRoiY = y;
        CurrentRoiWidth = width;
        CurrentRoiHeight = height;
        CurrentRoiIsVisible = isVisible;
        CurrentRoiCanvasWidth = MainImage.PixelSize.Width;
        CurrentRoiCanvasHeight = MainImage.PixelSize.Height;
    }

    [RelayCommand]
    private async Task AddRoi()
    {
        if (MainImage is null)
        {
            return;
        }

        if (CurrentRoiWidth <= 0 || CurrentRoiHeight <= 0)
        {
            return;
        }

        var roiName = await _dialogService.OpenAddRoiDialogAsync();

        if(roiName is null)
        {
            Debug.WriteLine("ROI 추가 취소됨");
            return;
        }

        Debug.WriteLine($"ROI 이름: {roiName ?? "취소됨"}");

        var xRatio = Math.Clamp(CurrentRoiX / MainImage.PixelSize.Width, 0d, 1d);
        var yRatio = Math.Clamp(CurrentRoiY / MainImage.PixelSize.Height, 0d, 1d);
        var widthRatio = Math.Clamp(CurrentRoiWidth / MainImage.PixelSize.Width, 0d, 1d);
        var heightRatio = Math.Clamp(CurrentRoiHeight / MainImage.PixelSize.Height, 0d, 1d);

        /// ROI 추가
        InspectionRegions.Add(new InspectionRegion
        {
            RegionName = $"{roiName}",
            XRatio = xRatio,
            YRatio = yRatio,
            WidthRatio = widthRatio,
            HeightRatio = heightRatio,
            X = CurrentRoiX,
            Y = CurrentRoiY,
            Width = CurrentRoiWidth,
            Height = CurrentRoiHeight
        });

        Debug.WriteLine($"Added ROI: X={CurrentRoiX}, Y={CurrentRoiY}, Width={CurrentRoiWidth}, Height={CurrentRoiHeight}");

        /// 그린 ROI 제거
        UpdateCurrentRoiState(0, 0, 0, 0, false);
    }

}