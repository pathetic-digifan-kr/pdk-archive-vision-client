using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdkOcrClient;
public partial class InspectionRegion : ObservableObject
{
    public string RegionId { get; set; } = Guid.NewGuid().ToString();
    public string RegionName { get; set; } = string.Empty; // 예: "바코드영역", "제조번호"

    // [핵심] 어떤 이미지 크기가 들어와도 100% 호환되는 정규화 좌표 (0.0 ~ 1.0)
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
    private string _ocrResult = string.Empty;
}