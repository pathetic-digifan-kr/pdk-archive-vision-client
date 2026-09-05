using System.Threading.Tasks;
using System.Collections.Generic;

namespace PdkOcrClient.Services;

public interface IDialogService
{
    // C# 비동기 Task 패킷으로 선택된 파일 경로를 리턴!
    Task<string?> OpenFileDialogAsync(string title);

    Task<string?> SaveFileDialogAsync(string title, string defaultFileName);

    Task<string?> OpenRoiTemplateNameDialogAsync();

    Task<RoiTemplate?> OpenRoiTemplateLoadDialogAsync();

    Task OpenImageDirectoryInspectionDialogAsync(
        IReadOnlyList<ImageDirectoryInspectionRoiOption> roiOptions,
        OcrClient ocrClient);
}
