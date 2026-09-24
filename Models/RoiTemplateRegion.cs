using System;

namespace PdkOcrClient.Models;

/// <summary>
/// ROI template persistence model. The template keeps the client-side ID
/// separately from the OCR request model so the API contract does not change.
/// </summary>
public class RoiTemplateRegion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = string.Empty;
    public string? FieldKey { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
