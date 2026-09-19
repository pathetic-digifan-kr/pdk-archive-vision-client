using System;
using System.Collections.Generic;

namespace PdkOcrClient.Models;

public class RoiTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? TargetImageFileName { get; set; }
    public List<RoiTemplateRegion> Regions { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
