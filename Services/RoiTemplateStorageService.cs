using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PdkOcrClient.Services
{
    // 2. 전체 템플릿 DTO
    public class RoiTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? TargetImageFileName { get; set; }
        public List<RoiModel> Regions { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // 3. AppData JSON 파일 처리 전담 서비스
    public class RoiTemplateStorageService
    {
        private readonly string _templateFolderPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public RoiTemplateStorageService()
        {
            // AppData/Roaming/PDKArchive/Templates 경로 세팅
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _templateFolderPath = Path.Combine(appData, "PDKArchive", "Templates");

            // 폴더가 없으면 무결점 자동 생성!
            Directory.CreateDirectory(_templateFolderPath);

            // 예쁘게 정렬된 JSON 출력을 위한 옵션
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        // AppData 경로 반환 메서드
        public string GetStoragePath() => _templateFolderPath;

        // JSON 저장
        public async Task SaveTemplateAsync(RoiTemplate template)
        {
            string filePath = Path.Combine(_templateFolderPath, $"{GetSafeFileName(template.Name)}.json");
            using var stream = File.Create(filePath);
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

        // 전체 JSON 불러오기
        public async Task<List<RoiTemplate>> LoadAllTemplatesAsync()
        {
            var templates = new List<RoiTemplate>();
            var files = Directory.GetFiles(_templateFolderPath, "*.json");

            foreach (var file in files)
            {
                using var stream = File.OpenRead(file);
                var template = await JsonSerializer.DeserializeAsync<RoiTemplate>(stream, _jsonOptions);
                if (template != null) templates.Add(template);
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
}
