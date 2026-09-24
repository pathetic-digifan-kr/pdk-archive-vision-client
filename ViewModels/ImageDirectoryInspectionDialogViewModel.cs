using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdkOcrClient.Models;
using PdkOcrClient.Services;

namespace PdkOcrClient.ViewModels;

public partial class ImageDirectoryInspectionDialogViewModel : ObservableObject
{
    private readonly Func<Task<string?>> _selectDirectoryAsync;
    private readonly Func<Task<string?>> _saveResultsAsync;
    private readonly Action _closeAction;
    private readonly OcrClient _ocrClient;
    private readonly TemplateReference? _template;
    private readonly List<OcrResultDocument> _inspectionResults = [];

    private static readonly JsonSerializerOptions ResultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

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
        Func<Task<string?>> saveResultsAsync,
        Action closeAction,
        TemplateReference? template = null)
    {
        RoiOptions = new ObservableCollection<ImageDirectoryInspectionRoiOption>(roiOptions);
        SelectedRoiOption = RoiOptions.FirstOrDefault();
        IsRoiConditionEnabled = SelectedRoiOption is not null;
        _ocrClient = ocrClient;
        _selectDirectoryAsync = selectDirectoryAsync;
        _saveResultsAsync = saveResultsAsync;
        _closeAction = closeAction;
        _template = template;
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
        _inspectionResults.Clear();

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
            if (item.ResultDocument is not null)
            {
                _inspectionResults.Add(item.ResultDocument);
            }

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
    private async Task SaveResults()
    {
        if (_inspectionResults.Count == 0)
        {
            StatusText = "먼저 디렉토리 검사를 실행하세요.";
            return;
        }

        var outputPath = await _saveResultsAsync();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        try
        {
            var batch = new OcrBatchResultDocument
            {
                Items = [.. _inspectionResults]
            };

            await File.WriteAllTextAsync(
                outputPath,
                JsonSerializer.Serialize(batch, ResultJsonOptions));

            StatusText = $"결과 저장 완료: {_inspectionResults.Count}개 이미지";
        }
        catch (Exception ex)
        {
            StatusText = $"결과 저장 실패: {ex.Message}";
        }
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
                    "정상",
                    resultDocument: CreateResultDocument(imagePath, []));
            }

            using var webpStream = ImageEncodingService.ConvertImageFileToWebpStream(imagePath);
            var response = await _ocrClient.SendOcrRequestAsync(
                webpStream,
                [SelectedRoiOption.ToRoiModel()],
                cancellationToken);

            var records = new List<OcrResultRecord>();
            var selectedResult = response?.parsed_data
                .FirstOrDefault(result => result.Id == SelectedRoiOption.Id);
            var ocrText = selectedResult?.Text?.Trim() ?? string.Empty;
            records.Add(CreateResultRecord(SelectedRoiOption.Id, selectedResult, ocrText));
            var expectedValue = ExpectedRoiValue.Trim();
            var isMatched = selectedResult is not null
                && string.Equals(ocrText, expectedValue, StringComparison.Ordinal);

            // 대상의 검사 조건을 만족할 경우 검사
            if (isMatched)
            {
                var remainRois = RoiOptions.Where(x => x != SelectedRoiOption).ToList();
                if (remainRois.Count > 0)
                {
                    var remainingResponse = await _ocrClient.SendOcrRequestAsync(
                        webpStream,
                        [.. remainRois.Select(x => x.ToRoiModel())],
                        cancellationToken
                    );

                    foreach (var roi in remainRois)
                    {
                        var result = remainingResponse?.parsed_data
                            .FirstOrDefault(item => item.Id == roi.Id);
                        var text = result?.Text?.Trim() ?? string.Empty;
                        records.Add(CreateResultRecord(roi.Id, result, text));
                    }
                }

                return new ImageInspectionItem(
                imagePath,
                width,
                height,
                true,
                $"통과 : {ocrText}",
                ocrText,
                expectedValue,
                true,
                CreateResultDocument(imagePath, records));
            }

            return new ImageInspectionItem(
                imagePath,
                width,
                height,
                true,
                $"불일치 결과 : {ocrText}",
                ocrText,
                expectedValue,
                false,
                CreateResultDocument(imagePath, records));

        }
        catch (Exception ex)
        {
            var errorRecords = RoiOptions.Select(roi => new OcrResultRecord
            {
                RegionId = roi.Id,
                Status = "error",
                ErrorMessage = ex.Message
            });

            return new ImageInspectionItem(
                imagePath,
                0,
                0,
                false,
                ex.Message,
                resultDocument: CreateResultDocument(imagePath, errorRecords));
        }
    }

    private OcrResultDocument CreateResultDocument(
        string imagePath,
        IEnumerable<OcrResultRecord> results)
    {
        return new OcrResultDocument
        {
            SourceImage = new SourceImageReference
            {
                Path = imagePath,
                FileName = Path.GetFileName(imagePath)
            },
            Template = _template,
            Regions = [.. RoiOptions.Select(roi => new OcrResultRegion
            {
                RegionId = roi.Id,
                Label = roi.DisplayName,
                FieldKey = roi.FieldKey,
                X = roi.X,
                Y = roi.Y,
                Width = roi.Width,
                Height = roi.Height
            })],
            Results = [.. results]
        };
    }

    private static OcrResultRecord CreateResultRecord(
        string regionId,
        OcrInfo? result,
        string text)
    {
        return new OcrResultRecord
        {
            RegionId = regionId,
            Text = text,
            Confidence = result?.Confidence,
            Status = result is null
                ? "missing"
                : string.IsNullOrWhiteSpace(text) ? "empty" : "recognized"
        };
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
    public OcrResultDocument? ResultDocument { get; }

    public ImageInspectionItem(
        string filePath,
        int width,
        int height,
        bool isValid,
        string statusText,
        string ocrText = "",
        string expectedValue = "",
        bool isConditionMatched = false,
        OcrResultDocument? resultDocument = null)
    {
        FilePath = filePath;
        IsValid = isValid;
        StatusText = statusText;
        OcrText = ocrText;
        ExpectedValue = expectedValue;
        IsConditionMatched = isConditionMatched;
        ResultDocument = resultDocument;
        SizeText = isValid ? $"{width} x {height}" : "-";
    }
}
