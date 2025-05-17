namespace ASE.API.Features.AnomalyDetection.Models;

public class DataAnomaly
{
    public int Id { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // e.g., "low", "medium", "high"
    public DateTime DetectedAt { get; set; }
    
    // Enhanced properties for better visualization and insights
    public decimal? AnomalyScore { get; set; } // Numerical score for the anomaly (0-100)
    public string? AffectedEntity { get; set; } // Dealer, Group, etc. that is affected
    public string? AffectedMetric { get; set; } // The specific metric/KPI affected
    public decimal? ActualValue { get; set; } // The actual value that was anomalous
    public decimal? ExpectedValue { get; set; } // What the value was expected to be
    public decimal? Threshold { get; set; } // The threshold that was exceeded
    public List<string>? RelatedCellAddresses { get; set; } // Cells involved in the anomaly
    public List<int>? RelatedAnomalies { get; set; } // IDs of related anomalies
    public BusinessImpact? BusinessImpact { get; set; } // Business impact assessment
    public string? RecommendedAction { get; set; } // Recommended action to address the anomaly
    public TimeRange? TimeRange { get; set; } // Time period over which anomaly was detected
} 