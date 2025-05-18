using ASE.API.Features.QueryBuilder.Models;
using ASE.API.Features.QueryBuilder.Services;
using System.Text;

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
            QueryBuilderService queryBuilderService,
            ILogger<SpeechToTextService> logger) =>
        {
            try
            {
                // Option 1: Directly from form file
                if (request.Form.Files.Count > 0)
                {
                    var audioFile = request.Form.Files[0];
                    
                    // Log incoming audio details
                    logger.LogInformation($"Processing audio file: {audioFile.FileName}, Size: {audioFile.Length} bytes, ContentType: {audioFile.ContentType}");
                    
                    if (audioFile.Length == 0)
                    {
                        return Results.BadRequest(new { Error = "Empty file", Message = "The uploaded audio file is empty" });
                    }
                    
                    // Extract audio format from file extension or content type
                    string audioFormat = Path.GetExtension(audioFile.FileName).TrimStart('.').ToLowerInvariant();
                    if (string.IsNullOrEmpty(audioFormat) && audioFile.ContentType.Contains("audio/"))
                    {
                        audioFormat = audioFile.ContentType.Replace("audio/", "").Split(';')[0].Trim();
                    }
                    
                    // Handle common MIME type variations
                    audioFormat = audioFormat switch
                    {
                        "x-wav" => "wav",
                        "wave" => "wav",
                        "x-m4a" => "m4a",
                        "mpeg" => "mp3",
                        "mp4" => "m4a",
                        "x-mp4" => "m4a",
                        _ => audioFormat
                    };
                    
                    if (string.IsNullOrEmpty(audioFormat))
                    {
                        audioFormat = "wav"; // Default to WAV if unknown
                    }
                    
                    logger.LogInformation($"Detected audio format: {audioFormat}");
                    
                    try
                    {
                        // Save stream to memory to ensure it can be fully read
                        using var memoryStream = new MemoryStream();
                        await audioFile.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        
                        // Transcribe speech to text
                        var (transcribedText, diagnosticInfo) = await speechToTextService.TranscribeSpeechWithDiagnosticsAsync(
                            memoryStream,
                            audioFormat);
                        
                        if (string.IsNullOrEmpty(transcribedText))
                        {
                            logger.LogWarning("Speech transcription returned empty result");
                            return Results.BadRequest(new { 
                                Error = "Failed to transcribe speech", 
                                Message = "The speech service couldn't recognize any speech in the provided audio.",
                                Diagnostics = diagnosticInfo
                            });
                        }
                        
                        // Generate query from transcribed text
                        var queryRequest = new QueryRequest
                        {
                            NaturalLanguageQuery = transcribedText,
                            QueryType = request.Form.ContainsKey("queryType") ? request.Form["queryType"].ToString() : "sql"
                        };
                        
                        var response = await queryBuilderService.GenerateQueryAsync(queryRequest);
                        
                        return Results.Ok(new
                        {
                            TranscribedText = transcribedText,
                            Query = response,
                            Diagnostics = diagnosticInfo
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing audio file");
                        return Results.BadRequest(new { 
                            Error = "Audio processing error", 
                            Message = $"Error processing audio file: {ex.Message}",
                            ExceptionType = ex.GetType().Name,
                            StackTrace = ex.StackTrace
                        });
                    }
                }
                
                // Option 2: From base64 encoded audio
                if (request.HasJsonContentType())
                {
                    var requestBody = await request.ReadFromJsonAsync<SpeechRequestModel>();
                    
                    if (requestBody == null || string.IsNullOrEmpty(requestBody.Base64Audio))
                    {
                        return Results.BadRequest(new { Error = "Invalid request", Message = "Missing audio data" });
                    }
                    
                    // Normalize the audio format
                    string audioFormat = (requestBody.AudioFormat ?? "wav").ToLowerInvariant();
                    logger.LogInformation($"Processing base64 encoded audio, format: {audioFormat}");
                    
                    try
                    {
                        using var stream = speechToTextService.GetStreamFromBase64(requestBody.Base64Audio);
                        
                        // Check if we have a valid stream
                        if (stream.Length == 0)
                        {
                            return Results.BadRequest(new { Error = "Invalid audio data", Message = "Could not decode the provided base64 audio data" });
                        }
                        
                        // Ensure stream position is at the beginning
                        stream.Position = 0;
                        
                        // Transcribe speech to text
                        var (transcribedText, diagnosticInfo) = await speechToTextService.TranscribeSpeechWithDiagnosticsAsync(
                            stream,
                            audioFormat);
                        
                        if (string.IsNullOrEmpty(transcribedText))
                        {
                            logger.LogWarning("Speech transcription returned empty result from base64 audio");
                            return Results.BadRequest(new { 
                                Error = "Failed to transcribe speech", 
                                Message = "The speech service couldn't recognize any speech in the provided audio.",
                                Diagnostics = diagnosticInfo
                            });
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
                            Query = response,
                            Diagnostics = diagnosticInfo
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing base64 audio");
                        return Results.BadRequest(new { 
                            Error = "Audio processing error", 
                            Message = $"Error processing base64 audio: {ex.Message}",
                            ExceptionType = ex.GetType().Name,
                            StackTrace = ex.StackTrace
                        });
                    }
                }
                
                return Results.BadRequest(new { Error = "Invalid request", Message = "No audio data provided" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing speech input");
                return Results.Problem(
                    title: "Speech Processing Error",
                    detail: $"Error processing speech input: {ex.Message}",
                    statusCode: 500
                );
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
        
        // Add a diagnostic endpoint for testing speech service
        app.MapPost("/api/query-builder/speech-test", async (
            HttpRequest request,
            SpeechToTextService speechToTextService,
            ILogger<SpeechToTextService> logger) =>
        {
            try
            {
                if (!request.Form.Files.Any())
                {
                    return Results.BadRequest("No audio file uploaded");
                }
                
                var audioFile = request.Form.Files[0];
                
                // Log incoming audio details
                logger.LogInformation($"Testing audio file: {audioFile.FileName}, Size: {audioFile.Length} bytes, ContentType: {audioFile.ContentType}");
                
                if (audioFile.Length == 0)
                {
                    return Results.BadRequest("The uploaded audio file is empty");
                }
                
                // Save to a temporary file for inspection
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"speech_debug_{Guid.NewGuid()}.wav");
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(fileStream);
                }
                
                // Extract audio format
                string audioFormat = Path.GetExtension(audioFile.FileName).TrimStart('.').ToLowerInvariant();
                if (string.IsNullOrEmpty(audioFormat) && audioFile.ContentType.Contains("audio/"))
                {
                    audioFormat = audioFile.ContentType.Replace("audio/", "").Split(';')[0].Trim();
                }
                
                audioFormat = audioFormat == "" ? "wav" : audioFormat;
                
                // Now test each approach
                using var memoryStream = new MemoryStream();
                await audioFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                var diagReport = new StringBuilder();
                diagReport.AppendLine($"Audio file: {audioFile.FileName}");
                diagReport.AppendLine($"Format: {audioFormat}");
                diagReport.AppendLine($"Size: {audioFile.Length} bytes");
                diagReport.AppendLine($"Content type: {audioFile.ContentType}");
                diagReport.AppendLine($"Temporary file path: {tempFilePath}");
                
                // Test WAV header
                memoryStream.Position = 0;
                bool isValidHeader = speechToTextService.IsWavHeaderValid(memoryStream);
                diagReport.AppendLine($"Valid WAV header: {isValidHeader}");
                
                // Try diagnostic method
                memoryStream.Position = 0;
                var (text, diagnostics) = await speechToTextService.TranscribeSpeechWithDiagnosticsAsync(memoryStream, audioFormat);
                
                diagReport.AppendLine($"Recognition result: {(string.IsNullOrEmpty(text) ? "FAILED" : "SUCCESS")}");
                diagReport.AppendLine($"Transcribed text: {text}");
                diagReport.AppendLine($"Diagnostics: {diagnostics}");
                
                return Results.Ok(new { 
                    Message = "Speech test completed", 
                    Report = diagReport.ToString(),
                    TranscribedText = text,
                    Diagnostics = diagnostics,
                    TempFile = tempFilePath
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in speech test");
                return Results.Problem(
                    title: "Speech Test Error",
                    detail: $"Error testing speech: {ex.Message}\n{ex.StackTrace}",
                    statusCode: 500
                );
            }
        })
        .WithName("SpeechTest")
        .WithDescription("Test endpoint for diagnosing speech recognition issues");
        
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