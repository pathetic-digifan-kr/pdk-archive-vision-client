using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdkOcrClient.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

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
    private readonly OcrClient _ocrClient;
    private readonly RoiTemplateStorageService _roiTemplateStorageService;

    public MainWindowViewModel(
        IDialogService dialogService,
        OcrClient? ocrClient = null,
        RoiTemplateStorageService? roiTemplateStorageService = null)
    {
        _dialogService = dialogService;
        _ocrClient = ocrClient ?? new OcrClient();
        _roiTemplateStorageService = roiTemplateStorageService ?? new RoiTemplateStorageService();
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

    [RelayCommand]
    private async Task SaveRoiTemplate()
    {
        if (InspectionRegions.Count == 0)
        {
            Debug.WriteLine("ROI template save canceled: no ROI regions.");
            return;
        }

        var templateName = await _dialogService.OpenRoiTemplateNameDialogAsync();
        if (templateName is null)
        {
            Debug.WriteLine("ROI template save canceled.");
            return;
        }

        var template = new RoiTemplate
        {
            Name = templateName,
            TargetImageFileName = SelectedFileName,
            Regions = InspectionRegions.Select(region => new RoiModel
            {
                Label = region.RegionName,
                X = region.XRatio,
                Y = region.YRatio,
                Width = region.WidthRatio,
                Height = region.HeightRatio
            }).ToList()
        };

        await _roiTemplateStorageService.SaveTemplateAsync(template);
        Debug.WriteLine($"Saved ROI template: {templateName}");
    }

    [RelayCommand]
    private async Task DoOcr()
    {
        if (MainImage is null)
        {
            Debug.WriteLine("OCR 실행 실패: 이미지가 로드되지 않았습니다.");
            return;
        }

        if (InspectionRegions.Count == 0)
        {
            Debug.WriteLine("OCR 실행 실패: ROI가 없습니다.");
            return;
        }

        try
        {
            using var webpStream = await ConvertBitmapToWebpAsync(MainImage);
            var roiModels = InspectionRegions.Select(region => new RoiModel
            {
                Label = region.RegionId,
                X = region.XRatio,
                Y = region.YRatio,
                Width = region.WidthRatio,
                Height = region.HeightRatio
            }).ToList();

            var response = await _ocrClient.SendOcrRequestAsync(webpStream, roiModels);

            foreach(var inspectionRegion in InspectionRegions)
            {
                inspectionRegion.OcrResult = response?.parsed_data?.Where(x => x.Id == inspectionRegion.RegionId)?.First()?.Text ?? "OCR 누락";
            }

            Debug.WriteLine($"OCR 응답: {response}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OCR 실행 실패: {ex.Message}");
        }
    }

    private static Task<MemoryStream> ConvertBitmapToWebpAsync(Bitmap bitmap)
    {
        var pngStream = new MemoryStream();
        bitmap.Save(pngStream);
        pngStream.Position = 0;

        using var image = Image.Load<Rgba32>(pngStream);
        var webpStream = new MemoryStream();
        image.Save(webpStream, new WebpEncoder { Quality = 90 });
        webpStream.Position = 0;

        return Task.FromResult(webpStream);
    }

}
