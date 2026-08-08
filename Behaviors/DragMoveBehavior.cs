using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace PdkOcrClient;

public class DragMoveBehavior : Behavior<Control>
{
    private Point _dragStart;
    private bool _dragging;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is null) return;
        AssociatedObject.PointerPressed += OnPressed;
        AssociatedObject.PointerMoved += OnMoved;
        AssociatedObject.PointerReleased += OnReleased;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is null) return;
        AssociatedObject.PointerPressed -= OnPressed;
        AssociatedObject.PointerMoved -= OnMoved;
        AssociatedObject.PointerReleased -= OnReleased;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        // Parent의 Parent인 이유는 ContentPresenter가 Canvas 안에 있고, 그 위에 SelectableRectangle이 있기 때문입니다.
        if(AssociatedObject?.Parent?.Parent is not Visual visual)
        {
            return;
        }
        _dragging = true;
        _dragStart = e.GetPosition(visual);

        e.Pointer.Capture(AssociatedObject);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || AssociatedObject is null) return;

        // Parent의 Parent인 이유는 ContentPresenter가 Canvas 안에 있고, 그 위에 SelectableRectangle이 있기 때문입니다.
        var target = AssociatedObject.Parent as Control ?? AssociatedObject;
        var pos = e.GetPosition(target.Parent as Visual);

        var newLeft = Canvas.GetLeft(target) + (pos.X - _dragStart.X);
        var newTop = Canvas.GetTop(target) + (pos.Y - _dragStart.Y);

        target.SetCurrentValue(Canvas.LeftProperty, newLeft);
        target.SetCurrentValue(Canvas.TopProperty, newTop);

        _dragStart = pos;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
    }
}