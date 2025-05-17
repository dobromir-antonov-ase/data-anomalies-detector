namespace ASE.API.Features.AnomalyDetection.Models;

public class DataAnomaly
{
    public int Id { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // e.g., "low", "medium", "high"
    public DateTime DetectedAt { get; set; }
} 