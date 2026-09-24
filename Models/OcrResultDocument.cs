using System;
using System.Collections.Generic;

namespace PdkOcrClient.Models;

public sealed class OcrResultDocument
{
    public int SchemaVersion { get; set; } = 1;
    public string ResultId { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public SourceImageReference SourceImage { get; set; } = new();
    public TemplateReference? Template { get; set; }
    public List<OcrResultRegion> Regions { get; set; } = [];
    public List<OcrResultRecord> Results { get; set; } = [];
}

public sealed class OcrBatchResultDocument
{
    public int SchemaVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<OcrResultDocument> Items { get; set; } = [];
}

public sealed class SourceImageReference
{
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public sealed class TemplateReference
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class OcrResultRegion
{
    public string RegionId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? FieldKey { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public sealed class OcrResultRecord
{
    public string RegionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public string Status { get; set; } = "missing";
    public string? ErrorMessage { get; set; }
}
