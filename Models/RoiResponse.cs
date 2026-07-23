namespace PdkOcrClient.Models;

public class RoiResponse
{
    public OcrInfo[] parsed_data {get; set;} = []; 
}

public class OcrInfo
{
    public string Id {get; set;} = string.Empty;

    public string Text {get; set;} = string.Empty;

    public double Confidence {get; set;} 
}