using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdkOcrClient.EventArgs;

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

    [RelayCommand]
    private void RegisterNewRegion(DrawingRectArgs args)
    {
        Debug.WriteLine($"Registering new region: X={args.X}, Y={args.Y}, Width={args.Width}, Height={args.Height}, CanvasWidth={args.CanvasActualWidth}, CanvasHeight={args.CanvasActualHeight}");
        //UpdateCurrentRoiState(args.X, args.Y, args.Width, args.Height, true, args.CanvasWidth, args.CanvasHeight);
    }

    public void UpdateCurrentRoiState(double x, double y, double width, double height, bool isVisible, double canvasWidth, double canvasHeight)
    {
        CurrentRoiX = x;
        CurrentRoiY = y;
        CurrentRoiWidth = width;
        CurrentRoiHeight = height;
        CurrentRoiIsVisible = isVisible;
        CurrentRoiCanvasWidth = canvasWidth > 0 ? canvasWidth : 1;
        CurrentRoiCanvasHeight = canvasHeight > 0 ? canvasHeight : 1;
    }

    [RelayCommand]
    private void AddRoi()
    {
        if (MainImage is null)
        {
            return;
        }

        if (CurrentRoiWidth <= 0 || CurrentRoiHeight <= 0)
        {
            return;
        }

        var canvasWidth = Math.Max(1d, CurrentRoiCanvasWidth);
        var canvasHeight = Math.Max(1d, CurrentRoiCanvasHeight);

        var xRatio = Math.Clamp(CurrentRoiX / canvasWidth, 0d, 1d);
        var yRatio = Math.Clamp(CurrentRoiY / canvasHeight, 0d, 1d);
        var widthRatio = Math.Clamp(CurrentRoiWidth / canvasWidth, 0d, 1d);
        var heightRatio = Math.Clamp(CurrentRoiHeight / canvasHeight, 0d, 1d);

        InspectionRegions.Add(new InspectionRegion
        {
            RegionName = $"ROI {InspectionRegions.Count + 1}",
            XRatio = xRatio,
            YRatio = yRatio,
            WidthRatio = widthRatio,
            HeightRatio = heightRatio,
            SourceImageWidth = MainImage.PixelSize.Width,
            SourceImageHeight = MainImage.PixelSize.Height,
        });

        Debug.WriteLine($"Added ROI: X={xRatio}, Y={yRatio}, Width={widthRatio}, Height={heightRatio}");

        UpdateCurrentRoiState(0, 0, 0, 0, false, canvasWidth, canvasHeight);
    }

}