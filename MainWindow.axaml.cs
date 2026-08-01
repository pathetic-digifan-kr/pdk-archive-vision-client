using System;
using Avalonia;
using Avalonia.Controls;

namespace PdkOcrClient;

public partial class MainWindow : Window
{
    public static readonly StyledProperty<InspectionRegion?> SelectedInspectionRegionProperty =
        AvaloniaProperty.Register<MainWindow, InspectionRegion?>(nameof(SelectedInspectionRegion));

    public InspectionRegion? SelectedInspectionRegion
    {
        get => GetValue(SelectedInspectionRegionProperty);
        set => SetValue(SelectedInspectionRegionProperty, value);
    }

    public SelectableRectangle? SelectedRectangle { get; private set; }

    private bool _isDrawing;
    private Point _roiStartPoint;
    private Point _roiCurrentPoint;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void RoiCanvas_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _isDrawing = false;
    }

    private void RoiCanvas_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_isDrawing) return;

        _roiCurrentPoint = e.GetPosition(RoiCanvas);
        UpdateRoiRectangleVisual();
    }

    private void RoiCanvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        _isDrawing = true;
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

    private void SelectableRectangle_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not SelectableRectangle selectedRectangle)
        {
            return;
        }

        if(SelectedRectangle == sender){
            return;
        }

        if (SelectedRectangle is not null && !ReferenceEquals(SelectedRectangle, selectedRectangle))
        {
            SelectedRectangle.IsSelected = false;
        }

        SelectedRectangle = selectedRectangle;
    }
}