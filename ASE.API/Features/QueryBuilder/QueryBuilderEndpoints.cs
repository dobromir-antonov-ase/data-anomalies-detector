using ASE.API.Features.QueryBuilder.Models;
using ASE.API.Features.QueryBuilder.Services;

namespace ASE.API.Features.QueryBuilder;

public static class QueryBuilderEndpoints
{
    public static IEndpointRouteBuilder MapQueryBuilderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/query-builder/generate", async (
            QueryRequest request,
            QueryBuilderService queryBuilderService) =>
        {
            var response = await queryBuilderService.GenerateQueryAsync(request);
            
            if (!response.IsSuccessful)
            {
                return Results.BadRequest(response);
            }
            
            return Results.Ok(response);
        })
        .WithName("GenerateQuery")
        .WithDescription("Generates a database query based on natural language input using AI");
        
        app.MapPost("/api/query-builder/speech", async (
            HttpRequest request,
            SpeechToTextService speechToTextService,
            QueryBuilderService queryBuilderService) =>
        {
            try
            {
                // Get audio content from request
                // Option 1: Directly from form file
                if (request.Form.Files.Count > 0)
                {
                    var audioFile = request.Form.Files[0];
                    using var stream = audioFile.OpenReadStream();
                    
                    // Transcribe speech to text
                    var transcribedText = await speechToTextService.TranscribeSpeechAsync(
                        stream,
                        Path.GetExtension(audioFile.FileName).TrimStart('.'));
                    
                    if (string.IsNullOrEmpty(transcribedText))
                    {
                        return Results.BadRequest("Failed to transcribe speech");
                    }
                    
                    // Generate query from transcribed text
                    var queryRequest = new QueryRequest
                    {
                        NaturalLanguageQuery = transcribedText,
                        QueryType = request.Form["queryType"].ToString() ?? "sql"
                    };
                    
                    var response = await queryBuilderService.GenerateQueryAsync(queryRequest);
                    
                    return Results.Ok(new
                    {
                        TranscribedText = transcribedText,
                        Query = response
                    });
                }
                
                // Option 2: From base64 encoded audio
                if (request.HasJsonContentType())
                {
                    var requestBody = await request.ReadFromJsonAsync<SpeechRequestModel>();
                    
                    if (requestBody == null || string.IsNullOrEmpty(requestBody.Base64Audio))
                    {
                        return Results.BadRequest("Invalid request: Missing audio data");
                    }
                    
                    using var stream = speechToTextService.GetStreamFromBase64(requestBody.Base64Audio);
                    
                    // Transcribe speech to text
                    var transcribedText = await speechToTextService.TranscribeSpeechAsync(
                        stream,
                        requestBody.AudioFormat ?? "wav");
                    
                    if (string.IsNullOrEmpty(transcribedText))
                    {
                        return Results.BadRequest("Failed to transcribe speech");
                    }
                    
                    // Generate query from transcribed text
                    var queryRequest = new QueryRequest
                    {
                        NaturalLanguageQuery = transcribedText,
                        QueryType = requestBody.QueryType ?? "sql"
                    };
                    
                    var response = await queryBuilderService.GenerateQueryAsync(queryRequest);
                    
                    return Results.Ok(new
                    {
                        TranscribedText = transcribedText,
                        Query = response
                    });
                }
                
                return Results.BadRequest("Invalid request: No audio data provided");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing speech input: {ex.Message}");
            }
        })
        .WithName("SpeechToQuery")
        .WithDescription("Transcribes speech to text and generates a database query from it");
        
        app.MapGet("/api/query-builder/query-types", () =>
        {
            // Return the supported query types
            return Results.Ok(new[] { "sql", "linq" });
        })
        .WithName("GetQueryTypes")
        .WithDescription("Returns the list of supported query types");
        
        return app;
    }
}

// Model for speech request with base64 encoded audio
public class SpeechRequestModel
{
    public string Base64Audio { get; set; } = string.Empty;
    public string? AudioFormat { get; set; }
    public string? QueryType { get; set; }
} 