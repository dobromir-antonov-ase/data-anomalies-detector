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
            string endpoint = _configuration["SpeechToText:Endpoint"] ?? "https://api.openai.com/v1/audio/transcriptions";
            
            // Mock implementation for development
            if (apiKey == "mock-key" || string.IsNullOrEmpty(apiKey))
            {
                return "Show me total sales for all dealers in the last quarter";
            }
            
            // Create multipart form content for the API request
            using var formContent = new MultipartFormDataContent();
            
            // Add the audio file
            // Copy the stream content first to ensure it can be read
            var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            var streamContent = new StreamContent(memoryStream);
            formContent.Add(streamContent, "file", $"audio.{audioFormat}");
            
            // Add other parameters - use whisper-1 model (OpenAI's recommended model for transcription)
            formContent.Add(new StringContent("whisper-1"), "model");
            formContent.Add(new StringContent("en"), "language"); // Default to English
            
            // Add API key to headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            // Send the request
            var response = await _httpClient.PostAsync(endpoint, formContent);
            
            // Handle the response
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Speech-to-text API returned {response.StatusCode}: {errorContent}");
                return string.Empty;
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Whisper API response: {responseBody}");
            
            // Parse the JSON response
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            
            // OpenAI Whisper API returns a JSON with a "text" field containing the transcription
            if (jsonResponse.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
            
            _logger.LogWarning("Unexpected API response format: {Response}", responseBody);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing speech");
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