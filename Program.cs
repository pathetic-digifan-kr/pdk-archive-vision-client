using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace PdkOcrClient;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var appBuilder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new FontManagerOptions
            {
                FontFallbacks =
                [
                    new FontFallback { FontFamily = new FontFamily("avares://PdkOcrClient/Assets/Fonts#NanumGothic") }
                ]
            });
            
        if(IsUbuntuWayland())
        {
            appBuilder = appBuilder.UseWayland();
        }
        else
        {
            appBuilder = appBuilder.UsePlatformDetect();
        }
                    
        return appBuilder;
    }
        
          

    public static bool IsUbuntuWayland()
    {
        if (!OperatingSystem.IsLinux()) 
            return false;

        // 1. 세션 타입 체크 (wayland)
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        
        // 2. Wayland 디스플레이 소켓 존재 여부 체크 (e.g., wayland-0)
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

        return string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase) 
            || !string.IsNullOrEmpty(waylandDisplay);
    }
}
