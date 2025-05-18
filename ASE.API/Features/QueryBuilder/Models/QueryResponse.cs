namespace ASE.API.Features.QueryBuilder.Models;

public class QueryResponse
{
    public string GeneratedQuery { get; set; } = string.Empty;
    public string QueryType { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public List<Dictionary<string, object>>? PreviewData { get; set; }
} 