using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PdkOcrClient.Services;

public class RoiTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? TargetImageFileName { get; set; }
    public List<RoiModel> Regions { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class RoiTemplateStorageService
{
    private readonly string _templateFolderPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public RoiTemplateStorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _templateFolderPath = Path.Combine(appData, "PDKArchive", "Templates");
        Directory.CreateDirectory(_templateFolderPath);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public string GetStoragePath() => _templateFolderPath;

    public string? GetTemplateImagePath(RoiTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.TargetImageFileName))
        {
            return null;
        }

        var imagePath = Path.Combine(_templateFolderPath, template.TargetImageFileName);
        return File.Exists(imagePath) ? imagePath : null;
    }

    public async Task SaveTemplateAsync(RoiTemplate template)
    {
        var filePath = Path.Combine(_templateFolderPath, $"{GetSafeFileName(template.Name)}.json");
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, template, _jsonOptions);
    }

    public async Task SaveTemplateAsync(RoiTemplate template, string? sourceImagePath)
    {
        if (!string.IsNullOrWhiteSpace(sourceImagePath) && File.Exists(sourceImagePath))
        {
            template.TargetImageFileName = await CopyTemplateImageAsync(template.Name, sourceImagePath);
        }

        await SaveTemplateAsync(template);
    }

    public async Task<List<RoiTemplate>> LoadAllTemplatesAsync()
    {
        var templates = new List<RoiTemplate>();
        var files = Directory.GetFiles(_templateFolderPath, "*.json");

        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var template = await JsonSerializer.DeserializeAsync<RoiTemplate>(stream, _jsonOptions);
            if (template is not null)
            {
                templates.Add(template);
            }
        }

        return templates;
    }

    private async Task<string> CopyTemplateImageAsync(string templateName, string sourceImagePath)
    {
        var extension = Path.GetExtension(sourceImagePath);
        var imageFileName = $"{GetSafeFileName(templateName)}{extension}";
        var destinationPath = Path.Combine(_templateFolderPath, imageFileName);

        if (Path.GetFullPath(sourceImagePath) == Path.GetFullPath(destinationPath))
        {
            return imageFileName;
        }

        await using var sourceStream = File.OpenRead(sourceImagePath);
        await using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream);

        return imageFileName;
    }

    private static string GetSafeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeFileName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();

        return string.IsNullOrWhiteSpace(safeFileName) ? "roi-template" : safeFileName;
    }
}
