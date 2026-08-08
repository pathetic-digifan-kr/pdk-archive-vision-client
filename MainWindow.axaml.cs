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
}