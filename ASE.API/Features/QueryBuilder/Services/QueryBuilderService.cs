using System.Text;
using System.Text.Json;
using ASE.API.Common.Data;
using ASE.API.Features.QueryBuilder.Models;
using Microsoft.EntityFrameworkCore;

namespace ASE.API.Features.QueryBuilder.Services;

public class QueryBuilderService
{
    private readonly FinanceDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueryBuilderService> _logger;
    private readonly HttpClient _httpClient;

    public QueryBuilderService(
        FinanceDbContext dbContext,
        IConfiguration configuration,
        ILogger<QueryBuilderService> logger,
        HttpClient httpClient)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<QueryResponse> GenerateQueryAsync(QueryRequest request)
    {
        try
        {
            // 1. Gather database schema information
            var schemaInfo = await GetDatabaseSchemaInfo();
            
            // 2. Construct prompt for AI
            var prompt = ConstructAIPrompt(request, schemaInfo);
            
            // 3. Call AI service to generate query
            var generatedQuery = await CallOpenAIServiceAsync(prompt, request.QueryType);
            
            // 4. Optional: Execute query to get preview data
            var previewData = await TryExecuteQueryAsync(generatedQuery, request.QueryType);
            
            return new QueryResponse
            {
                GeneratedQuery = generatedQuery,
                QueryType = request.QueryType,
                Explanation = "Query generated based on database schema and natural language input.",
                IsSuccessful = true,
                PreviewData = previewData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating query");
            return new QueryResponse
            {
                IsSuccessful = false,
                ErrorMessage = $"Failed to generate query: {ex.Message}"
            };
        }
    }

    private async Task<string> GetDatabaseSchemaInfo()
    {
        var entityTypes = _dbContext.Model.GetEntityTypes().ToList();
        var schemaBuilder = new StringBuilder();
        
        foreach (var entityType in entityTypes)
        {
            var entityName = entityType.ClrType.Name;
            schemaBuilder.AppendLine($"Entity: {entityName}");
            
            // Properties/columns
            schemaBuilder.AppendLine("Properties:");
            foreach (var property in entityType.GetProperties())
            {
                schemaBuilder.AppendLine($"  - {property.Name}: {property.ClrType.Name}");
            }
            
            // Relationships
            schemaBuilder.AppendLine("Relationships:");
            foreach (var navigation in entityType.GetNavigations())
            {
                var targetType = navigation.TargetEntityType.ClrType.Name;
                var isCollection = navigation.IsCollection;
                schemaBuilder.AppendLine($"  - {navigation.Name} -> {targetType} ({(isCollection ? "many" : "one")})");
            }
            
            schemaBuilder.AppendLine();
        }
        
        return schemaBuilder.ToString();
    }
    
    private string ConstructAIPrompt(QueryRequest request, string schemaInfo)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("# Database Schema Information");
        promptBuilder.AppendLine(schemaInfo);
        
        promptBuilder.AppendLine("# Query Request");
        promptBuilder.AppendLine($"Natural Language Query: {request.NaturalLanguageQuery}");
        promptBuilder.AppendLine($"Query Type: {request.QueryType}");
        if (!string.IsNullOrEmpty(request.TargetEntity))
        {
            promptBuilder.AppendLine($"Target Entity: {request.TargetEntity}");
        }
        
        promptBuilder.AppendLine("# Task");
        promptBuilder.AppendLine($"Generate a {request.QueryType.ToUpper()} query based on the natural language request and the database schema.");
        promptBuilder.AppendLine("The query should be valid, efficient, and directly executable against the database.");
        promptBuilder.AppendLine("Only return the generated query, no explanation or additional text.");
        
        return promptBuilder.ToString();
    }
    
    private async Task<string> CallOpenAIServiceAsync(string prompt, string queryType)
    {
        // Get configuration for OpenAI API
        string apiKey = _configuration["AIService:ApiKey"] ?? "mock-key";
        string endpoint = _configuration["AIService:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        
        // Mock implementation for development - in reality you'd send the request to an AI model
        if (apiKey == "mock-key" || string.IsNullOrEmpty(apiKey))
        {
            // This is just a placeholder. In a real implementation, you would send a request to an AI model.
            return MockGenerateQuery(prompt, queryType);
        }

        // Set up the request to OpenAI Chat Completions API
        var requestData = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new { 
                    role = "system", 
                    content = "You are a database query expert that generates precise database queries based on natural language requests and database schemas."
                },
                new {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.2,
            max_tokens = 1000
        };
        
        var content = new StringContent(
            JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            "application/json");
            
        // Add OpenAI API key to headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        
        try
        {
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API returned {response.StatusCode}: {errorContent}");
                throw new Exception($"OpenAI API returned {response.StatusCode}");
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            // Parse the OpenAI response
            return ExtractGeneratedQueryFromOpenAIResponse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            // Fallback to mock if API call fails
            return $"-- API call failed: {ex.Message}\n" + MockGenerateQuery(prompt, queryType);
        }
    }
    
    private string MockGenerateQuery(string prompt, string queryType)
    {
        // This is a placeholder implementation for development/testing
        // In a real system, this would come from an actual AI model
        
        if (prompt.Contains("total sales", StringComparison.OrdinalIgnoreCase))
        {
            return queryType.ToLower() switch
            {
                "sql" => "SELECT d.Name, SUM(fsc.Value) AS TotalSales FROM FinanceSubmissionCells fsc " +
                         "JOIN FinanceSubmissions fs ON fsc.FinanceSubmissionId = fs.Id " +
                         "JOIN Dealers d ON fs.DealerId = d.Id " +
                         "WHERE fsc.GlobalAddress LIKE 'Income Statement!%' " +
                         "AND fsc.CellAddress IN ('B4', 'C4', 'D4') " +
                         "GROUP BY d.Name " +
                         "ORDER BY TotalSales DESC;",
                         
                "linq" => @"var results = from fsc in _dbContext.SubmissionData
                           join fs in _dbContext.FinanceSubmissions on fsc.FinanceSubmissionId equals fs.Id
                           join d in _dbContext.Dealers on fs.DealerId equals d.Id
                           where fsc.GlobalAddress.StartsWith(""Income Statement!"") &&
                                 (fsc.CellAddress == ""B4"" || fsc.CellAddress == ""C4"" || fsc.CellAddress == ""D4"")
                           group fsc by d.Name into g
                           select new { DealerName = g.Key, TotalSales = g.Sum(x => x.Value) };",
                           
                _ => "Query type not supported"
            };
        }
        
        if (prompt.Contains("anomalies", StringComparison.OrdinalIgnoreCase))
        {
            return queryType.ToLower() switch
            {
                "sql" => "SELECT da.Id, da.Description, da.Severity, d.Name AS DealerName " +
                         "FROM DataAnomalies da " +
                         "JOIN FinanceSubmissions fs ON da.FinanceSubmissionId = fs.Id " +
                         "JOIN Dealers d ON fs.DealerId = d.Id " +
                         "WHERE da.Severity > 3 " +
                         "ORDER BY da.Severity DESC;",
                         
                "linq" => @"var results = from da in _dbContext.DataAnomalies
                           join fs in _dbContext.FinanceSubmissions on da.FinanceSubmissionId equals fs.Id
                           join d in _dbContext.Dealers on fs.DealerId equals d.Id
                           where da.Severity > 3
                           orderby da.Severity descending
                           select new { da.Id, da.Description, da.Severity, DealerName = d.Name };",
                           
                _ => "Query type not supported"
            };
        }
        
        return "-- No specific query pattern matched. This is a placeholder for AI-generated content.";
    }
    
    private string ExtractGeneratedQueryFromOpenAIResponse(string openAIResponse)
    {
        try
        {
            // Parse the JSON response from OpenAI
            var responseObject = JsonSerializer.Deserialize<JsonElement>(openAIResponse);
            
            // Extract the message content from OpenAI's response structure
            if (responseObject.TryGetProperty("choices", out var choices) && 
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
                }
            }
            
            _logger.LogWarning("Unexpected OpenAI response format: {Response}", openAIResponse);
            return "-- Could not extract query from AI response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing OpenAI response");
            return "-- Error parsing AI response.";
        }
    }
    
    private async Task<List<Dictionary<string, object>>?> TryExecuteQueryAsync(string query, string queryType)
    {
        // This is a placeholder for actually executing the query to get preview data
        // In a real implementation, you would:
        // 1. Parse the query
        // 2. Execute it safely (with limits)
        // 3. Return a preview of the results
        
        // For now, we'll return null as this requires complex implementation
        // that would depend on the specific query and database
        return null;
    }
} 