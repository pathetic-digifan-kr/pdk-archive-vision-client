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
    private InspectionRegion? _boundRegion;

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
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundRegion is not null)
        {
            _boundRegion.PropertyChanged -= OnBoundRegionPropertyChanged;
        }

        _boundRegion = DataContext as InspectionRegion;

        if (_boundRegion is not null)
        {
            SetCurrentValue(IsSelectedProperty, _boundRegion.IsSelected);
            _boundRegion.PropertyChanged += OnBoundRegionPropertyChanged;
        }
        else
        {
            SetCurrentValue(IsSelectedProperty, false);
        }
    }

    private void OnBoundRegionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InspectionRegion.IsSelected))
        {
            SetCurrentValue(IsSelectedProperty, _boundRegion?.IsSelected ?? false);
        }
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