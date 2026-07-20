namespace PdkOcrClient;
public class RoiModel
{
    public string Label {get; set;} = string.Empty;
    public double X {get; set;}
    public double Y {get; set;}

    public double Width {get; set;}
    public double Height {get; set;}
}