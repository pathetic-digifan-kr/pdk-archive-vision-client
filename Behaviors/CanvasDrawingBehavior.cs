using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Avalonia;
using System.Windows.Input;
using PdkOcrClient.EventArgs;

namespace PdkOcrClient.Behaviors;

public class CanvasDrawingBehavior : Behavior<Canvas>
{
    // ViewModel의 Command를 바인딩받을 의존성 프로퍼티(DependencyProperty) 뙇!
    public static readonly AvaloniaProperty<ICommand> RegionRegisteredCommandProperty =
        AvaloniaProperty.Register<CanvasDrawingBehavior, ICommand>(nameof(RegionRegisteredCommand));

    public ICommand? RegionRegisteredCommand
    {
        get => GetValue(RegionRegisteredCommandProperty) as ICommand;
        set => SetValue(RegionRegisteredCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        // 캔버스가 마운트되면 이벤트 구독 스타트!!!
        AssociatedObject?.AddHandler(InputElement.PointerReleasedEvent, OnCanvasMouseUp);
    }

    protected override void OnDetaching()
    {
        // 메모리 누수(Memory Leak) 방지용 가비지 컬렉터 가드 해제!!!
        AssociatedObject?.RemoveHandler(InputElement.PointerReleasedEvent, OnCanvasMouseUp);
        base.OnDetaching();
    }

    private void OnCanvasMouseUp(object? sender, PointerReleasedEventArgs e)
    {
        if (RegionRegisteredCommand == null) return;

        if (AssociatedObject is not Canvas canvas) return;

        // 1. Behavior가 달라붙은 Canvas 객체에 다이렉트 접근해서 Actual 크기 뙇!
        double actualWidth = canvas.Bounds.Width;
        double actualHeight = canvas.Bounds.Height;

        // 2. 마우스 최종 좌표 뙇! (오빠의 드래그 사각형 크기 연산 로직 믹인)
        Point currentPos = e.GetPosition(AssociatedObject);

        // 3. 순수 데이터 DTO로 덤프 쳐서 뷰모델 커맨드로 바이패스 격발!!!
        var args = new DrawingRectArgs(currentPos.X, currentPos.Y, 150, 100, actualWidth, actualHeight);
        
        if (RegionRegisteredCommand.CanExecute(args))
        {
            RegionRegisteredCommand.Execute(args);
        }
    }
}