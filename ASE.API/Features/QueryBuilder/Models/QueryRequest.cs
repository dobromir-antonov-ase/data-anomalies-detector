namespace ASE.API.Features.QueryBuilder.Models;

public class QueryRequest
{
    public string NaturalLanguageQuery { get; set; } = string.Empty;
    public string QueryType { get; set; } = "sql"; // Options: sql, linq, etc.
    public string? TargetEntity { get; set; } // Optional target entity if known
} 