namespace ASE.API.Features.AnomalyDetection.Models;

public class DataPattern
{
    public int Id { get; set; }
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Significance { get; set; } = string.Empty; // e.g., "low", "medium", "high"
    public decimal ConfidenceScore { get; set; } // 0-100 confidence percentage
    public DateTime DetectedAt { get; set; }
    
    // Additional statistical properties
    public decimal? Correlation { get; set; } // For correlation patterns
    public string? Formula { get; set; } // For mathematical relationships
    public decimal? R2Value { get; set; } // For regression patterns
    public List<string>? RelatedCellAddresses { get; set; } // Cells involved in the pattern
    
    // Navigation properties for enhanced features
    public TimeRange? TimeRange { get; set; } // Time period over which pattern was detected
    public IndustryComparison? IndustryComparison { get; set; } // Comparison with industry averages
    public BusinessImpact? BusinessImpact { get; set; } // Business impact assessment
    public List<int>? RelatedPatterns { get; set; } // IDs of related patterns
} 