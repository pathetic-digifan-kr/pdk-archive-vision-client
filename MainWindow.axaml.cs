using System;
using Avalonia;
using Avalonia.Controls;

namespace PdkOcrClient;

public partial class MainWindow : Window
{
    private enum RoiInteractionState
    {
        None,
        DrawingNewRegion,
        MovingRegion
    }

    private RoiInteractionState _state = RoiInteractionState.None;

    private Point _roiStartPoint;
    private Point _roiCurrentPoint;

    public static readonly StyledProperty<object?> SelectedRectangleProperty =
        AvaloniaProperty.Register<MainWindow, object?>(nameof(SelectedRectangle));

    public object? SelectedRectangle
    {
        get => GetValue(SelectedRectangleProperty);
        set => SetValue(SelectedRectangleProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
    }


    private void SelectableRectangle_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not SelectableRectangle selectedRectangle)
        {
            return;
        }

        SetCurrentValue(SelectedRectangleProperty, selectedRectangle.DataContext);
    }

    private void RoiCanvas_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _state = RoiInteractionState.None;
    }

    private void RoiCanvas_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_state != RoiInteractionState.DrawingNewRegion) return;

        _roiCurrentPoint = e.GetPosition(RoiCanvas);
        UpdateRoiRectangleVisual();
    }

    private void RoiCanvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        _state = RoiInteractionState.DrawingNewRegion;
        _roiStartPoint = e.GetPosition(RoiCanvas);
        _roiCurrentPoint = _roiStartPoint;
        UpdateRoiRectangleVisual();
    }


    private void UpdateRoiRectangleVisual()
    {
        var x = Math.Min(_roiStartPoint.X, _roiCurrentPoint.X);
        var y = Math.Min(_roiStartPoint.Y, _roiCurrentPoint.Y);
        var width = Math.Abs(_roiCurrentPoint.X - _roiStartPoint.X);
        var height = Math.Abs(_roiCurrentPoint.Y - _roiStartPoint.Y);

        Canvas.SetLeft(RoiRectangle, x);
        Canvas.SetTop(RoiRectangle, y);
        RoiRectangle.Width = width;
        RoiRectangle.Height = height;
        RoiRectangle.IsVisible = width > 0 && height > 0;
    }
}
