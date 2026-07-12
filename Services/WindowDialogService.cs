using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

public class WindowDialogService : IDialogService
{
    public async Task<string?> OpenFileDialogAsync(string title)
    {
        // 1. 현재 구동 중인 데스크톱 라이프타임 어플리케이션의 메인 윈도우 뷰포트 스캔!
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            && desktop.MainWindow is Window mainWindow)
        {
            // 2. 메인 윈도우로부터 스토리지 프로바이더 인터페이스 락 해제!
            var storageProvider = TopLevel.GetTopLevel(mainWindow)?.StorageProvider;
            if (storageProvider == null) return null;

            // 3. 아발로니아 최신 스펙의 FilePicker 오픈 옵션 마운트!
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll } // 이미지 확장자 가드 오토 필터링!
            });

            // 4. 선택된 파일 패킷이 존재하면 로컬 절대 경로 파싱해서 리턴!
            if (files.Any())
            {
                return files[0].Path.LocalPath;
            }
        }
        return null;
    }
}