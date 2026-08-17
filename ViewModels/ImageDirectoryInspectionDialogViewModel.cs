using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdkOcrClient.Services;
using SkiaSharp;

namespace PdkOcrClient.ViewModels;

public partial class ImageDirectoryInspectionDialogViewModel : ObservableObject
{
    private readonly Func<Task<string?>> _selectDirectoryAsync;
    private readonly Action _closeAction;
    private readonly OcrClient _ocrClient;

    [ObservableProperty]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    private string _searchPatterns = "*.png;*.jpg;*.jpeg;*.webp;*.bmp";

    [ObservableProperty]
    private bool _includeSubdirectories = true;

    [ObservableProperty]
    private int _maxImageCount = 500;

    [ObservableProperty]
    private string _statusText = "검사할 디렉토리를 선택하세요.";

    [ObservableProperty]
    private bool _isRoiConditionEnabled;

    [ObservableProperty]
    private bool _showOnlyExpectedMatches = true;

    [ObservableProperty]
    private ImageDirectoryInspectionRoiOption? _selectedRoiOption;

    [ObservableProperty]
    private string _expectedRoiValue = string.Empty;

    public ObservableCollection<ImageInspectionItem> Images { get; } = [];

    public ObservableCollection<ImageDirectoryInspectionRoiOption> RoiOptions { get; }

    public ImageDirectoryInspectionDialogViewModel(
        IReadOnlyList<ImageDirectoryInspectionRoiOption> roiOptions,
        OcrClient ocrClient,
        Func<Task<string?>> selectDirectoryAsync,
        Action closeAction)
    {
        RoiOptions = new ObservableCollection<ImageDirectoryInspectionRoiOption>(roiOptions);
        SelectedRoiOption = RoiOptions.FirstOrDefault();
        IsRoiConditionEnabled = SelectedRoiOption is not null;
        _ocrClient = ocrClient;
        _selectDirectoryAsync = selectDirectoryAsync;
        _closeAction = closeAction;
    }

    [RelayCommand]
    private async Task SelectDirectory()
    {
        var directoryPath = await _selectDirectoryAsync();
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        DirectoryPath = directoryPath;
    }

    [RelayCommand]
    private async Task InspectDirectory()
    {
        Images.Clear();

        if (string.IsNullOrWhiteSpace(DirectoryPath) || !Directory.Exists(DirectoryPath))
        {
            StatusText = "유효한 디렉토리를 선택하세요.";
            return;
        }

        var patterns = SearchPatterns
            .Split([';', ',', '\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .DefaultIfEmpty("*.*")
            .ToArray();

        var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var imagePaths = patterns
            .SelectMany(pattern => EnumerateFiles(DirectoryPath, pattern, searchOption))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, MaxImageCount))
            .ToList();

        var inspectedCount = 0;
        var matchedCount = 0;
        foreach (var imagePath in imagePaths)
        {
            inspectedCount++;
            StatusText = $"검사 중: {inspectedCount} / {imagePaths.Count}";

            var item = await InspectImageAsync(imagePath, CancellationToken.None);
            if (item.IsConditionMatched)
            {
                matchedCount++;
            }

            if (!IsRoiConditionEnabled || !ShowOnlyExpectedMatches || item.IsConditionMatched)
            {
                Images.Add(item);
            }
        }

        var validCount = Images.Count(item => item.IsValid);
        StatusText = IsRoiConditionEnabled
            ? $"검사 완료: {inspectedCount}개 중 {matchedCount}개 조건 일치, 목록 {Images.Count}개 표시"
            : $"검사 완료: {Images.Count}개 중 {validCount}개 이미지 확인";
    }

    [RelayCommand]
    private void Close()
    {
        _closeAction();
    }

    private async Task<ImageInspectionItem> InspectImageAsync(string imagePath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = File.OpenRead(imagePath);
            using var bitmap = new Bitmap(stream);
            var width = bitmap.PixelSize.Width;
            var height = bitmap.PixelSize.Height;

            if (!IsRoiConditionEnabled || SelectedRoiOption is null)
            {
                return new ImageInspectionItem(
                    imagePath,
                    width,
                    height,
                    true,
                    "정상");
            }

            using var webpStream = ConvertImageToWebpStream(imagePath);
            var response = await _ocrClient.SendOcrRequestAsync(
                webpStream,
                [SelectedRoiOption.ToRoiModel()],
                cancellationToken);

            var ocrText = response?.parsed_data
                .FirstOrDefault(result => result.Id == SelectedRoiOption.Id)
                ?.Text
                ?.Trim() ?? string.Empty;
            var expectedValue = ExpectedRoiValue.Trim();
            var isMatched = string.Equals(ocrText, expectedValue, StringComparison.Ordinal);
            var statusText = isMatched ? "조건 일치" : "조건 불일치";

            return new ImageInspectionItem(
                imagePath,
                width,
                height,
                true,
                statusText,
                ocrText,
                expectedValue,
                isMatched);
        }
        catch (Exception ex)
        {
            return new ImageInspectionItem(imagePath, 0, 0, false, ex.Message);
        }
    }

    private static string[] EnumerateFiles(string directoryPath, string pattern, SearchOption searchOption)
    {
        try
        {
            return Directory.GetFiles(directoryPath, pattern, searchOption);
        }
        catch
        {
            return [];
        }
    }

    private static MemoryStream ConvertImageToWebpStream(string imagePath)
    {
        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap is null)
        {
            throw new InvalidOperationException("이미지를 디코딩할 수 없습니다.");
        }

        var stream = new MemoryStream();
        bitmap.Encode(stream, SKEncodedImageFormat.Webp, 100);
        stream.Position = 0;

        return stream;
    }
}

public sealed class ImageInspectionItem
{
    public string FilePath { get; }
    public string FileName => Path.GetFileName(FilePath);
    public string DirectoryName => Path.GetDirectoryName(FilePath) ?? string.Empty;
    public string SizeText { get; }
    public string StatusText { get; }
    public string OcrText { get; }
    public string ExpectedValue { get; }
    public bool IsValid { get; }
    public bool IsConditionMatched { get; }

    public ImageInspectionItem(
        string filePath,
        int width,
        int height,
        bool isValid,
        string statusText,
        string ocrText = "",
        string expectedValue = "",
        bool isConditionMatched = false)
    {
        FilePath = filePath;
        IsValid = isValid;
        StatusText = statusText;
        OcrText = ocrText;
        ExpectedValue = expectedValue;
        IsConditionMatched = isConditionMatched;
        SizeText = isValid ? $"{width} x {height}" : "-";
    }
}
