using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdkOcrClient.Models;

public partial class InspectionRegion : ObservableObject
{
    public string RegionId { get; set; } = Guid.NewGuid().ToString();

    public double XRatio { get; set; }
    public double YRatio { get; set; }
    public double WidthRatio { get; set; }
    public double HeightRatio { get; set; }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isEditingName;

    [ObservableProperty]
    private string _regionName = string.Empty;

    [ObservableProperty]
    private string? _fieldKey;

    [ObservableProperty]
    private string _ocrResult = string.Empty;

    [ObservableProperty]
    private double? _ocrConfidence;

    [ObservableProperty]
    private string _ocrStatus = "missing";

    [ObservableProperty]
    private string? _ocrErrorMessage;

    [RelayCommand]
    private void StartEditingName()
    {
        IsEditingName = true;
    }

    [RelayCommand]
    private void CompleteEditingName()
    {
        IsEditingName = false;
        RegionName = RegionName.Trim();
    }
}
