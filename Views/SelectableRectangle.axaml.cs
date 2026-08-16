using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace PdkOcrClient;

public partial class SelectableRectangle : UserControl
{
    private double _pivotX;

    private double _pivotY;

    private bool _isDragging;

    private enum ResizeDirection
    {
        BottomRight,
        BottomCenter,
        BottomLeft,
        CenterRight,
        CenterLeft,
        TopRight,
        TopCenter,
        TopLeft,
    }

    private ResizeDirection _direction;


    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SelectableRectangle, bool>(nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public SelectableRectangle()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", e.GetNewValue<bool>());
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SetCurrentValue(IsSelectedProperty, true);
    }

    private Control? GetCanvasChild(Control? start)
    {
        Visual? current = start;
        while (current is not null && current is not Canvas)
        {
            var parent = current.GetVisualParent();
            if (parent is Canvas)
                return current as Control; // Canvas 바로 아래에 있는 요소를 반환
            current = parent;
        }
        return null;
    }

    private bool TryGetCanvasContext(out Control? container, out Canvas? canvas)
    {
        container = GetCanvasChild(this);
        if (container?.GetVisualParent() is not Canvas resolvedCanvas)
        {
            canvas = null;
            return false;
        }

        canvas = resolvedCanvas;
        return true;
    }

    private void ApplyResizeFromPointer(Control container, Canvas canvas, Point mouse)
    {
        // X좌표 시작점과 끝 점
        var left = Math.Min(_pivotX, mouse.X);
        var right = Math.Max(_pivotX, mouse.X);

        // Y좌표 시작점과 끝 점
        var top = Math.Min(_pivotY, mouse.Y);
        var bottom = Math.Max(_pivotY, mouse.Y);

        // 중앙의 좌 우 핸들은 Y축을 변경하지 않는다.
        if (_direction is ResizeDirection.CenterLeft or ResizeDirection.CenterRight)
        {
            top = Canvas.GetTop(container);
            bottom = top + Height;
        }
        // 중앙의 상 하 핸들은 X축을 변경하지 않는다.
        else if (_direction is ResizeDirection.TopCenter or ResizeDirection.BottomCenter)
        {
            left = Canvas.GetLeft(container);
            right = left + Width;
        }

        // 범위 제한
        left = Math.Clamp(left, 0, canvas.Bounds.Width);
        top = Math.Clamp(top, 0, canvas.Bounds.Height);
        right = Math.Clamp(right, 0, canvas.Bounds.Width);
        bottom = Math.Clamp(bottom, 0, canvas.Bounds.Height);

        // 위치 설정
        if (Canvas.GetLeft(container) != left)
            container.SetCurrentValue(Canvas.LeftProperty, left);

        if (Canvas.GetTop(container) != top)
            container.SetCurrentValue(Canvas.TopProperty, top);

        // 크기 설정
        SetCurrentValue(WidthProperty, right - left);
        SetCurrentValue(HeightProperty, bottom - top);
    }

    private void Handle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging)
            return;

        if (!TryGetCanvasContext(out var container, out var canvas))
            return;

        ApplyResizeFromPointer(
            container!,
            canvas!,
            e.GetPosition(canvas));

        e.Handled = true;
    }

    private void Handle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(sender is not Control control)
        {
            return;
        }

        if(!Enum.TryParse<ResizeDirection>(control.Tag?.ToString(), out var direction))
        {
            return;
        }


        if (!TryGetCanvasContext(out var container, out var canvas))
            return;

        // 좌측의 점들을 잡은 경우 Pivot의 X 좌표가 우측으로 변경된다.
        if(direction == ResizeDirection.BottomLeft || direction == ResizeDirection.CenterLeft || direction == ResizeDirection.TopLeft)
        {
            _pivotX = Canvas.GetLeft(container!) + Width;
        }
        // 그 외의 경우엔 Pivot의 X좌표는 좌측이다.
        else
        {
            _pivotX = Canvas.GetLeft(container!);
        }

        // 상단의 점을 잡은 경우 pivot의 Y 좌표는 하단이다.
        if(direction == ResizeDirection.TopRight || direction == ResizeDirection.TopCenter || direction == ResizeDirection.TopLeft)
        {
            _pivotY = Canvas.GetTop(container!) + Height;
        }
        // 그 외의 경우 Pivot의 Y좌표는 상단이다.
        else
        {
            _pivotY = Canvas.GetTop(container!);
        }

        // Canvas 기준 마우스상의 현재 절대 좌표
        _direction = direction;
        _isDragging = true;

        e.Pointer.Capture(sender as Control);
        e.Handled = true;
    }

    private void Handle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}