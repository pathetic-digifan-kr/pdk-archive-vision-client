using System.Threading.Tasks;

public interface IDialogService
{
    // C# 비동기 Task 패킷으로 선택된 파일 경로를 리턴!
    Task<string?> OpenFileDialogAsync(string title);
}