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

    private bool _isDragging;

    private Point _startPoint;


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

    private void BottomRightHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        var container = GetCanvasChild(this);

        if (!_isDragging)
            return;

        if (container?.GetVisualParent() is not Canvas canvas)
            return;

        var newPos = e.GetPosition(canvas);

        // 너비와 높이는 시작위치 기준으로 본래 너비, 높이에 위치 변화량을 더한 절대값
        Point dPos = newPos - _startPoint;
        double newWidth = Math.Abs(_startWidth + dPos.X);
        double newHeight = Math.Abs(_startHeight + dPos.Y);

        bool isLeft = newPos.X < _startLeft;
        bool isTop = newPos.Y < _startTop;

        // 시작위치 기준 마우스가 좌측으로 넘어가는 경우 좌측으로 평행이동
        if (isLeft)
        {
            container.SetCurrentValue(Canvas.LeftProperty, newPos.X);
        }

        // 시작위치 기준 마우스가 위로 넘어가는 경우 위로 평행이동
        if (isTop)
        {
            container.SetCurrentValue(Canvas.TopProperty, newPos.Y);
        }

        this.SetCurrentValue(WidthProperty, newWidth);
        this.SetCurrentValue(HeightProperty, newHeight);

        e.Handled = true;
    }

    private void BottomRightHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var container = GetCanvasChild(this);

        if (container?.GetVisualParent() is not Canvas canvas)
            return;

        // Canvas 기준 마우스상의 현재 절대 좌표
        _startPoint = e.GetPosition(canvas);
        _startLeft = Canvas.GetLeft(container);
        _startTop = Canvas.GetTop(container);
        _isDragging = true;

        e.Pointer.Capture(sender as Control);
        e.Handled = true;

        _startWidth = Width;
        _startHeight = Height;

        Debug.WriteLine("Press");
    }

    private void BottomRightHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}