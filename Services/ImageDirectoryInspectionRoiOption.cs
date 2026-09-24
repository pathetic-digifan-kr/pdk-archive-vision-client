using PdkOcrClient.Models;

namespace PdkOcrClient.Services;

public sealed record ImageDirectoryInspectionRoiOption(
    string Id,
    string Name,
    string? FieldKey,
    double X,
    double Y,
    double Width,
    double Height)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Id : Name;

    public RoiModel ToRoiModel() => new()
    {
        Label = Id,
        X = X,
        Y = Y,
        Width = Width,
        Height = Height
    };
}
