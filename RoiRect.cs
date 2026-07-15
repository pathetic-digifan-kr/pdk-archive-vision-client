using System;

namespace PdkOcrClient;
public class InspectionRegion
{
    public string RegionId { get; set; } = Guid.NewGuid().ToString();
    public string RegionName { get; set; } = string.Empty; // 예: "바코드영역", "제조번호"

    // [핵심] 어떤 이미지 크기가 들어와도 100% 호환되는 정규화 좌표 (0.0 ~ 1.0)
    public double XRatio { get; set; }
    public double YRatio { get; set; }
    public double WidthRatio { get; set; }
    public double HeightRatio { get; set; }

    // [선택형 메타데이터] 이 영역을 '처음 생성할 당시'의 가로세로 비율 (종횡비 체크용)
    public double CreatedAspectRatio => (double)SourceImageWidth / SourceImageHeight;
    public int SourceImageWidth { get; set; }
    public int SourceImageHeight { get; set; }
}