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
        UpdateSelectionVisualState();
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

        UpdateSelectionVisualState();
    }

    private void OnBoundRegionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InspectionRegion.IsSelected))
        {
            UpdateSelectionVisualState();
            SetCurrentValue(IsSelectedProperty, _boundRegion?.IsSelected ?? false);
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsSelectedProperty)
        {
            UpdateSelectionVisualState();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SetCurrentValue(IsSelectedProperty, true);
    }

    private void UpdateSelectionVisualState()
    {
        var border = this.FindControl<Border>("SelectionBorder");
        var fill = this.FindControl<Rectangle>("RegionFill");
        var highlight = this.FindControl<Rectangle>("SelectionHighlight");
        var resizeHandles = this.FindControl<Grid>("SelectionHandles");

        var isSelected = IsSelected;

        var selectedBorderBrush = new SolidColorBrush(Color.Parse("#38BDF8"));
        var defaultBorderBrush = new SolidColorBrush(Color.Parse("#2563EB"));
        var selectedFillBrush = new SolidColorBrush(Color.Parse("#38BDF8"));
        var defaultFillBrush = new SolidColorBrush(Color.Parse("#2563EB"));

        if (border is not null)
        {
            border.BorderBrush = isSelected ? selectedBorderBrush : defaultBorderBrush;
            border.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
            border.Padding = isSelected ? new Thickness(3) : new Thickness(2);
        }

        if (fill is not null)
        {
            fill.Stroke = isSelected ? selectedFillBrush : defaultFillBrush;
            fill.Fill = isSelected ? selectedFillBrush : defaultFillBrush;
            fill.Opacity = isSelected ? 0.24 : 0.18;
            fill.StrokeThickness = isSelected ? 3 : 2;
        }

        if (highlight is not null)
        {
            highlight.IsVisible = isSelected;
        }

        if (resizeHandles is not null)
        {
            resizeHandles.IsVisible = isSelected;
        }
    }
}