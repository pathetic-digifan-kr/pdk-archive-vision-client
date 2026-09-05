using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdkOcrClient.Services;

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

    public InspectionRegion? SelectedInspectionRegion
    {
        get => _selectedInspectionRegion;
        set
        {
            var prevInspectionRegion = _selectedInspectionRegion;
            if (SetProperty(ref _selectedInspectionRegion, value))
            {
                if(prevInspectionRegion is not null && prevInspectionRegion.IsSelected)
                {
                    prevInspectionRegion.IsSelected = false;
                }

                if(value is not null && !value.IsSelected)
                {
                    value.IsSelected = true;
                }
            }
        }
    }
    private InspectionRegion? _selectedInspectionRegion;

    private readonly IDialogService _dialogService;
    private readonly OcrClient _ocrClient;
    private readonly RoiTemplateStorageService _roiTemplateStorageService;
    private string? _selectedFilePath;

    private JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

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
                LoadImage(filePath);
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

        string roiName = $"ROI {InspectionRegions.Count + 1}";


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

        /// ROI 위치 비율 업데이트
        UpdateRoiRatiosFromPixels();

        var template = new RoiTemplate
        {
            Name = templateName,
            Regions = [.. InspectionRegions.Select(region => new RoiModel
            {
                Label = region.RegionName,
                X = region.XRatio,
                Y = region.YRatio,
                Width = region.WidthRatio,
                Height = region.HeightRatio
            })]
        };

        await _roiTemplateStorageService.SaveTemplateAsync(template, _selectedFilePath);
        Debug.WriteLine($"Saved ROI template: {templateName}");
    }

    [RelayCommand]
    private async Task LoadRoiTemplate()
    {
        var template = await _dialogService.OpenRoiTemplateLoadDialogAsync();
        if (template is null)
        {
            Debug.WriteLine("ROI template load canceled.");
            return;
        }

        var imagePath = _roiTemplateStorageService.GetTemplateImagePath(template);
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            LoadImage(imagePath);
        }

        if (MainImage is null)
        {
            Debug.WriteLine("ROI template load failed: no image is available.");
            return;
        }

        InspectionRegions.Clear();

        foreach (var region in template.Regions)
        {
            var xRatio = Math.Clamp(region.X, 0d, 1d);
            var yRatio = Math.Clamp(region.Y, 0d, 1d);
            var widthRatio = Math.Clamp(region.Width, 0d, 1d);
            var heightRatio = Math.Clamp(region.Height, 0d, 1d);
            var regionName = string.IsNullOrWhiteSpace(region.Label)
                ? $"ROI {InspectionRegions.Count + 1}"
                : region.Label;

            InspectionRegions.Add(new InspectionRegion
            {
                RegionId = regionName,
                RegionName = regionName,
                XRatio = xRatio,
                YRatio = yRatio,
                WidthRatio = widthRatio,
                HeightRatio = heightRatio,
                X = xRatio * MainImage.PixelSize.Width,
                Y = yRatio * MainImage.PixelSize.Height,
                Width = widthRatio * MainImage.PixelSize.Width,
                Height = heightRatio * MainImage.PixelSize.Height
            });
        }

        UpdateCurrentRoiState(0, 0, 0, 0, false);
        Debug.WriteLine($"Loaded ROI template: {template.Name}");
    }

    [RelayCommand]
    private async Task OpenImageDirectoryInspection()
    {
        UpdateRoiRatiosFromPixels();

        var roiOptions = InspectionRegions
            .Select(region => new ImageDirectoryInspectionRoiOption(
                region.RegionId,
                region.RegionName,
                region.XRatio,
                region.YRatio,
                region.WidthRatio,
                region.HeightRatio))
            .ToList();

        await _dialogService.OpenImageDirectoryInspectionDialogAsync(roiOptions, _ocrClient);
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
            UpdateRoiRatiosFromPixels();

            using var webpStream = await ImageEncodingService.ConvertBitmapToWebpAsync(MainImage);
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

    [RelayCommand]
    private async Task SaveOcrResult()
    {
        if (InspectionRegions.Count == 0)
        {
            Debug.WriteLine("OCR 결과 저장 실패: ROI가 없습니다.");
            return;
        }

        var defaultFileName = string.IsNullOrWhiteSpace(SelectedFileName)
            ? "ocr-results.json"
            : $"{Path.GetFileNameWithoutExtension(SelectedFileName)}-ocr-results.json";
        var filePath = await _dialogService.SaveFileDialogAsync("OCR 결과 저장", defaultFileName);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.WriteLine("OCR 결과 저장이 취소되었습니다.");
            return;
        }

        try
        {
            // 중복 방지로 groupBy를 사용하여 마지막 OCR 결과만 저장
            var ocrResults = InspectionRegions
            .GroupBy(region => region.RegionName)
            .ToDictionary(
                group => group.Key,
                group => group.Last().OcrResult);

            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, ocrResults, _jsonSerializerOptions);

            Debug.WriteLine($"OCR 결과 저장 완료: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OCR 결과 저장 실패: {ex.Message}");
        }
    }

    private void LoadImage(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        MainImage?.Dispose();

        MainImage = new Bitmap(stream);
        SelectedFileName = Path.GetFileName(filePath);
        _selectedFilePath = filePath;
        CurrentRoiCanvasWidth = MainImage.PixelSize.Width;
        CurrentRoiCanvasHeight = MainImage.PixelSize.Height;
        UpdateRoiPixelsFromRatios();
    }

    private void UpdateRoiPixelsFromRatios()
    {
        if (MainImage is null)
        {
            return;
        }

        var imageWidth = MainImage.PixelSize.Width;
        var imageHeight = MainImage.PixelSize.Height;

        foreach (var region in InspectionRegions)
        {
            var xRatio = Math.Clamp(region.XRatio, 0d, 1d);
            var yRatio = Math.Clamp(region.YRatio, 0d, 1d);
            var widthRatio = Math.Clamp(region.WidthRatio, 0d, 1d - xRatio);
            var heightRatio = Math.Clamp(region.HeightRatio, 0d, 1d - yRatio);

            region.XRatio = xRatio;
            region.YRatio = yRatio;
            region.WidthRatio = widthRatio;
            region.HeightRatio = heightRatio;
            region.X = xRatio * imageWidth;
            region.Y = yRatio * imageHeight;
            region.Width = widthRatio * imageWidth;
            region.Height = heightRatio * imageHeight;
        }
    }

    private void UpdateRoiRatiosFromPixels()
    {
        if(MainImage is null)
        {
            return;
        }

        foreach(var region in InspectionRegions)
        {
            var xRatio = Math.Clamp(region.X / MainImage.PixelSize.Width, 0d, 1d);
            var yRatio = Math.Clamp(region.Y / MainImage.PixelSize.Height, 0d, 1d);
            var widthRatio = Math.Clamp(region.Width / MainImage.PixelSize.Width, 0d, 1d);
            var heightRatio = Math.Clamp(region.Height / MainImage.PixelSize.Height, 0d, 1d);

            region.XRatio = xRatio;
            region.YRatio = yRatio;
            region.WidthRatio = widthRatio;
            region.HeightRatio = heightRatio;
        }
    }

}
