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

    public static readonly StyledProperty<object?> SelectedRectangleProperty =
        AvaloniaProperty.Register<MainWindow, object?>(nameof(SelectedRectangle));

    public object? SelectedRectangle
    {
        get => GetValue(SelectedRectangleProperty);
        set => SetValue(SelectedRectangleProperty, value);
    }

    private RoiInteractionState _interactionState = RoiInteractionState.None;
    private InspectionRegion? _movingRegion;
    private Point _roiStartPoint;
    private Point _roiCurrentPoint;
    private Point _interactionStartPoint;
    private Point _movingRegionStartPosition;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void RoiCanvas_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _interactionState = RoiInteractionState.None;
        e.Pointer.Capture(null);
    }

    private void RoiCanvas_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_interactionState == RoiInteractionState.DrawingNewRegion)
        {
            _roiCurrentPoint = e.GetPosition(RoiCanvas);
            e.Pointer.Capture(RoiCanvas);
            UpdateRoiRectangleVisual();
            return;
        }

        if (_interactionState == RoiInteractionState.MovingRegion && _movingRegion is not null)
        {
            var currentPoint = e.GetPosition(RoiCanvas);
            var deltaX = currentPoint.X - _interactionStartPoint.X;
            var deltaY = currentPoint.Y - _interactionStartPoint.Y;

            var nextX = _movingRegionStartPosition.X + deltaX;
            var nextY = _movingRegionStartPosition.Y + deltaY;
            var maxX = Math.Max(0, RoiCanvas.Bounds.Width - _movingRegion.Width);
            var maxY = Math.Max(0, RoiCanvas.Bounds.Height - _movingRegion.Height);

            _movingRegion.X = Math.Clamp(nextX, 0, maxX);
            _movingRegion.Y = Math.Clamp(nextY, 0, maxY);
            e.Pointer.Capture(RoiCanvas);
        }
    }

    private void RoiCanvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.Source != RoiCanvas)
        {
            return;
        }

        _interactionState = RoiInteractionState.DrawingNewRegion;
        _movingRegion = null;
        SetCurrentValue(SelectedRectangleProperty, null);

        _roiStartPoint = e.GetPosition(RoiCanvas);
        _roiCurrentPoint = _roiStartPoint;
        e.Pointer.Capture(RoiCanvas);

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

    private void SelectableRectangle_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not SelectableRectangle selectedRectangle)
        {
            return;
        }

        var region = selectedRectangle.DataContext as InspectionRegion;
        if (region is null)
        {
            return;
        }

        _interactionState = RoiInteractionState.MovingRegion;
        _movingRegion = region;
        _interactionStartPoint = e.GetPosition(RoiCanvas);
        _movingRegionStartPosition = new Point(region.X, region.Y);

        SetCurrentValue(SelectedRectangleProperty, region);
        e.Pointer.Capture(RoiCanvas);
        e.Handled = true;
    }
}