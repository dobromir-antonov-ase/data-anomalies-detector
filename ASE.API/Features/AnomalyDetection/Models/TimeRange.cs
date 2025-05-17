namespace ASE.API.Features.AnomalyDetection.Models;

public class TimeRange
{
    public int Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    
    // Foreign keys
    public int? DataAnomalyId { get; set; }
    public int? DataPatternId { get; set; }
    
    // Navigation properties
    public DataAnomaly? DataAnomaly { get; set; }
    public DataPattern? DataPattern { get; set; }
} 