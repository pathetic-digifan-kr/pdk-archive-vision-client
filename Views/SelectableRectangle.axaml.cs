using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace PdkOcrClient;

public partial class SelectableRectangle : UserControl
{
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
}