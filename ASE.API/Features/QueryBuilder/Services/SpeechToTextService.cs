using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ASE.API.Features.QueryBuilder.Services;

public class SpeechToTextService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SpeechToTextService> _logger;
    private readonly HttpClient _httpClient;

    public SpeechToTextService(
        IConfiguration configuration,
        ILogger<SpeechToTextService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> TranscribeSpeechAsync(Stream audioStream, string audioFormat = "wav")
    {
        try
        {
            // Read API configuration
            string apiKey = _configuration["SpeechToText:ApiKey"] ?? "mock-key";
            string endpoint = _configuration["SpeechToText:Endpoint"] ?? "https://api.anthropic.com/v1/messages";
            
            // Mock implementation for development
            if (apiKey == "mock-key")
            {
                return "Show me total sales for all dealers in the last quarter";
            }
            
            // First, convert the audio to base64
            string base64Audio = await ConvertStreamToBase64(audioStream);
            
            // Create the messages request with audio content
            var requestData = new
            {
                model = "claude-3-opus-20240229",
                max_tokens = 1000,
                messages = new[]
                {
                    new {
                        role = "user",
                        content = new []
                        {
                            new {
                                type = "image",
                                source = new {
                                    type = "base64",
                                    media_type = $"audio/{audioFormat}",
                                    data = base64Audio
                                }
                            },
                            new {
                                type = "text",
                                text = "Please transcribe this audio recording accurately. It contains a database query request."
                            }
                        }
                    }
                },
                system = "You are an audio transcription expert. Your task is to accurately transcribe the spoken content in the audio file."
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");
                
            // Add Anthropic API key to headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            
            // Send the request
            var response = await _httpClient.PostAsync(endpoint, content);
            
            // Handle the response
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Anthropic API returned {response.StatusCode}");
                return string.Empty;
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            // Parse the Anthropic response to extract the transcription
            return ExtractTranscriptionFromAnthropicResponse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing speech");
            return string.Empty;
        }
    }
    
    private async Task<string> ConvertStreamToBase64(Stream stream)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
    
    private string ExtractTranscriptionFromAnthropicResponse(string response)
    {
        try
        {
            // Parse the JSON response
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
            
            // Extract the content text from the response
            if (jsonResponse.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array &&
                content.GetArrayLength() > 0)
            {
                var firstContent = content[0];
                if (firstContent.TryGetProperty("text", out var textElement))
                {
                    string fullText = textElement.GetString() ?? string.Empty;
                    
                    // Remove any explanations, keeping only the transcription
                    // This is a simple approach - in a real app you might need more sophisticated parsing
                    string[] markers = { "Transcription:", "Here's the transcription:", "Transcript:" };
                    foreach (var marker in markers)
                    {
                        int index = fullText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            fullText = fullText.Substring(index + marker.Length).Trim();
                            break;
                        }
                    }
                    
                    // Remove any closing comments
                    string[] endMarkers = { "End of transcription", "End of transcript", "That's all" };
                    foreach (var marker in endMarkers)
                    {
                        int index = fullText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            fullText = fullText.Substring(0, index).Trim();
                            break;
                        }
                    }
                    
                    return fullText;
                }
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Anthropic response for transcription");
            return string.Empty;
        }
    }
    
    // Method to prepare an audio stream from a base64 encoded string
    public Stream GetStreamFromBase64(string base64Audio)
    {
        try
        {
            byte[] audioBytes = Convert.FromBase64String(base64Audio);
            return new MemoryStream(audioBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting base64 to stream");
            return new MemoryStream();
        }
    }
} 