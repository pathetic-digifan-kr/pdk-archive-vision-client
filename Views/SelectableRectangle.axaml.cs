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
    private double _startLeft;

    private double _startTop;

    private double _startWidth;

    private double _startHeight;

    private double _pivotX;

    private double _pivotY;

    private bool _isDragging;

    private Point _startPoint;

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

    private void ApplyResizeFromPointer(Control container, Canvas canvas, Point newPos)
    {
        // 너비와 높이는 시작위치 기준으로 본래 너비, 높이에 위치 변화량을 더한 절대값
        double newWidth = Math.Abs(_pivotX - newPos.X);
        double newHeight = Math.Abs(_pivotY - newPos.Y);

        // X, Y 좌표 기본 값은 본래 좌표
        var newLeft = _pivotX;
        var newTop = _pivotY;

        // 현재 좌표
        var curLeft = Canvas.GetLeft(container);
        var curTop = Canvas.GetTop(container);

        // 시작위치 기준 마우스가 좌측으로 넘어가는 경우 좌측으로 평행이동
        if (newPos.X < _pivotX)
        {
            newLeft = Math.Clamp(newPos.X, 0, _pivotX);
            newWidth = Math.Clamp(newWidth, 0, _pivotX);
        }
        else
        {
            newWidth = Math.Clamp(newWidth, 0, canvas.Bounds.Width - newLeft);
        }


        // 시작위치 기준 마우스가 위로 넘어가는 경우 위로 평행이동
        if (newPos.Y < _pivotY)
        {
            newTop = Math.Clamp(newPos.Y, 0, _pivotY);
            newHeight = Math.Clamp(newHeight, 0, _pivotY);
        }
        else
        {
            newHeight = Math.Clamp(newHeight, 0, canvas.Bounds.Height - newTop);
        }

        // 좌표값이 바뀐 경우에 한해 평행이동
        if (curLeft != newLeft)
        {
            container.SetCurrentValue(Canvas.LeftProperty, newLeft);
        }

        if (curTop != newTop)
        {
            container.SetCurrentValue(Canvas.TopProperty, newTop);
        }

        this.SetCurrentValue(WidthProperty, newWidth);
        this.SetCurrentValue(HeightProperty, newHeight);
    }

    private void Handle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging)
            return;

        if (!TryGetCanvasContext(out var container, out var canvas))
            return;

        var newPos = e.GetPosition(canvas!);

        if(_direction == ResizeDirection.CenterRight || _direction == ResizeDirection.CenterLeft)
        {
            newPos = new Point(newPos.X, _pivotY + Height);
        }
        else if(_direction == ResizeDirection.BottomCenter || _direction == ResizeDirection.TopCenter)
        {
            newPos = new Point(_pivotX + Width, newPos.Y);
        }

        ApplyResizeFromPointer(container!, canvas!, newPos);

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

        if(direction == ResizeDirection.BottomRight || direction == ResizeDirection.BottomCenter || direction == ResizeDirection.CenterRight || direction == ResizeDirection.TopRight || direction == ResizeDirection.TopCenter)
        {
            _pivotX = Canvas.GetLeft(container!);
        }
        else if(direction == ResizeDirection.BottomLeft || direction == ResizeDirection.CenterLeft || direction == ResizeDirection.TopLeft)
        {
            _pivotX = Canvas.GetLeft(container!) + Width;
        }

        if(direction == ResizeDirection.BottomRight || direction == ResizeDirection.BottomLeft || direction == ResizeDirection.BottomCenter || direction == ResizeDirection.CenterRight || direction == ResizeDirection.CenterLeft)
        {
            _pivotY = Canvas.GetTop(container!);
        }
        else if(direction == ResizeDirection.TopRight || direction == ResizeDirection.TopCenter || direction == ResizeDirection.TopLeft)
        {
            _pivotY = Canvas.GetTop(container!) + Height;
        }

        // Canvas 기준 마우스상의 현재 절대 좌표
        _startPoint = e.GetPosition(canvas!);
        _startLeft = Canvas.GetLeft(container!);
        _startTop = Canvas.GetTop(container!);
        _direction = direction;
        _isDragging = true;

        _direction = direction;
        e.Pointer.Capture(sender as Control);
        e.Handled = true;

        _startWidth = Width;
        _startHeight = Height;

        Debug.WriteLine(direction);
    }

    private void Handle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}