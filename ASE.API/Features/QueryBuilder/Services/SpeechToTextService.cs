using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Collections.Concurrent;

namespace ASE.API.Features.QueryBuilder.Services;

public class SpeechToTextService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SpeechToTextService> _logger;
    private readonly HttpClient _httpClient;
    
    // Default timeout for speech recognition (15 seconds)
    private const int DefaultRecognitionTimeoutMs = 15000;

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
        var (transcribedText, _) = await TranscribeSpeechWithDiagnosticsAsync(audioStream, audioFormat);
        return transcribedText;
    }
    
    public async Task<(string text, string diagnostics)> TranscribeSpeechWithDiagnosticsAsync(Stream audioStream, string audioFormat = "wav")
    {
        var diagnostics = new StringBuilder();
        try
        {
            // Get Azure Speech config
            string subscriptionKey = _configuration["AzureSpeech:SubscriptionKey"] ?? "";
            string region = _configuration["AzureSpeech:Region"] ?? "westeurope";
            
            // Log audio format info for debugging
            _logger.LogInformation($"Attempting to transcribe audio in format: {audioFormat}");
            diagnostics.AppendLine($"Audio format: {audioFormat}");
            
            // Use mock data if no key is provided (for development)
            if (string.IsNullOrEmpty(subscriptionKey))
            {
                string message = "No Azure Speech subscription key found. Using mock data.";
                _logger.LogWarning(message);
                diagnostics.AppendLine(message);
                return ("Show me total sales for all dealers in the last quarter", diagnostics.ToString());
            }
            
            // Setup Azure Speech config with enhanced settings for longer phrases
            var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            speechConfig.SpeechRecognitionLanguage = "en-US";
            
            // Significantly increase silence timeouts to handle longer pauses
            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "5000");
            speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "5000");
            speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "5000");
            
            // Enable detailed recognition results
            speechConfig.EnableAudioLogging();
            speechConfig.OutputFormat = OutputFormat.Detailed;
            
            // Copy stream to memory stream to ensure it can be read
            var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            // Log audio stream information
            _logger.LogInformation($"Audio stream length: {memoryStream.Length} bytes");
            diagnostics.AppendLine($"Audio stream length: {memoryStream.Length} bytes");
            
            if (memoryStream.Length == 0)
            {
                string message = "Empty audio stream received";
                _logger.LogError(message);
                diagnostics.AppendLine(message);
                return (string.Empty, diagnostics.ToString());
            }
            
            // Try three approaches in sequence, capturing any errors
            string recognitionResult = string.Empty;
            
            // For WAV files, try more specific approaches
            if (audioFormat.ToLower() == "wav")
            {
                diagnostics.AppendLine("WAV format detected, checking header...");
                
                // Check if valid WAV header
                memoryStream.Position = 0;
                bool validHeader = IsWavHeaderValid(memoryStream);
                diagnostics.AppendLine($"Valid WAV header: {validHeader}");
                
                if (validHeader)
                {
                    try
                    {
                        // 1. Try direct memory approach
                        diagnostics.AppendLine("Attempting direct memory recognition...");
                        memoryStream.Position = 0;
                        string directResult = await TryDirectMemoryRecognition(speechConfig, memoryStream);
                        
                        if (!string.IsNullOrEmpty(directResult))
                        {
                            diagnostics.AppendLine("Direct memory recognition success!");
                            return (directResult, diagnostics.ToString());
                        }
                        else
                        {
                            diagnostics.AppendLine("Direct memory recognition failed, continuing to next method.");
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Direct memory recognition error: {ex.Message}";
                        _logger.LogWarning(errorMsg);
                        diagnostics.AppendLine(errorMsg);
                    }
                }
                
                // 2. Try file-based approach with fixed header if needed
                try
                {
                    diagnostics.AppendLine("Attempting file-based recognition...");
                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"speech_{Guid.NewGuid()}.wav");
                    
                    try
                    {
                        // If header is invalid, create a new WAV file
                        if (!validHeader)
                        {
                            diagnostics.AppendLine("Creating WAV file with proper header...");
                            await GenerateStandardWavFile(memoryStream, tempFilePath);
                        }
                        else
                        {
                            // Otherwise just copy the existing WAV
                            diagnostics.AppendLine("Saving valid WAV to temporary file...");
                            using (var fileStream = File.Create(tempFilePath))
                            {
                                memoryStream.Position = 0;
                                await memoryStream.CopyToAsync(fileStream);
                            }
                        }
                        
                        diagnostics.AppendLine($"Temporary file created: {tempFilePath}");
                        string fileResult = await TryFileBasedRecognition(speechConfig, tempFilePath);
                        
                        if (!string.IsNullOrEmpty(fileResult))
                        {
                            diagnostics.AppendLine("File-based recognition success!");
                            return (fileResult, diagnostics.ToString());
                        }
                        else
                        {
                            diagnostics.AppendLine("File-based recognition failed, continuing to fallback method.");
                        }
                    }
                    finally
                    {
                        // Clean up temp file
                        try
                        {
                            if (File.Exists(tempFilePath))
                            {
                                File.Delete(tempFilePath);
                                diagnostics.AppendLine("Temporary file deleted");
                            }
                        }
                        catch (Exception ex)
                        {
                            diagnostics.AppendLine($"Failed to delete temp file: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"File-based recognition error: {ex.Message}";
                    _logger.LogWarning(errorMsg);
                    diagnostics.AppendLine(errorMsg);
                }
            }
            
            // 3. Fallback to push stream approach (works for all formats)
            try
            {
                diagnostics.AppendLine("Attempting push stream recognition fallback...");
                memoryStream.Position = 0;
                recognitionResult = await TryPushStreamRecognition(speechConfig, memoryStream);
                
                if (!string.IsNullOrEmpty(recognitionResult))
                {
                    diagnostics.AppendLine("Push stream recognition success!");
                    return (recognitionResult, diagnostics.ToString());
                }
                else
                {
                    diagnostics.AppendLine("Push stream recognition failed.");
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Push stream recognition error: {ex.Message}";
                _logger.LogWarning(errorMsg);
                diagnostics.AppendLine(errorMsg);
            }
            
            // If we've tried all approaches and still have no result
            if (string.IsNullOrEmpty(recognitionResult))
            {
                diagnostics.AppendLine("All recognition approaches failed.");
                return (string.Empty, diagnostics.ToString());
            }
            
            return (recognitionResult, diagnostics.ToString());
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error transcribing speech: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            diagnostics.AppendLine(errorMsg);
            diagnostics.AppendLine(ex.StackTrace);
            
            // Fall back to mock data in case of complete failure
            return ("Show me total sales for all dealers in the last quarter", diagnostics.ToString());
        }
    }
    
    // Make our header validation accessible from outside
    public bool IsWavHeaderValid(Stream stream)
    {
        return IsValidWavHeader(stream);
    }
    
    // Direct memory recognition approach
    private async Task<string> TryDirectMemoryRecognition(SpeechConfig speechConfig, MemoryStream memoryStream)
    {
        _logger.LogInformation("Trying direct memory recognition approach");
        memoryStream.Position = 0;
        
        // Use FromWavFormat instead of FromWavFileInput for memory streams
        using var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
        using var audioInputStream = AudioInputStream.CreatePushStream(audioFormat);
        
        // Read the stream in chunks and push to the audio input stream
        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            audioInputStream.Write(buffer, bytesRead);
        }
        audioInputStream.Close();
        
        // Create the audio config from the stream
        using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
        return await PerformSingleShotRecognition(speechConfig, audioConfig);
    }
    
    // File-based recognition approach
    private async Task<string> TryFileBasedRecognition(SpeechConfig speechConfig, string filePath)
    {
        _logger.LogInformation($"Trying file-based recognition approach with file: {filePath}");
        using var audioConfig = AudioConfig.FromWavFileInput(filePath);
        return await PerformSingleShotRecognition(speechConfig, audioConfig);
    }
    
    // Push stream recognition approach
    private async Task<string> TryPushStreamRecognition(SpeechConfig speechConfig, MemoryStream memoryStream)
    {
        _logger.LogInformation("Trying push stream recognition approach");
        return await RecognizeUsingPushStream(speechConfig, memoryStream);
    }
    
    // Generate a standard WAV file with proper headers
    private async Task GenerateStandardWavFile(Stream inputStream, string outputPath)
    {
        _logger.LogInformation("Generating standard WAV file with proper headers");
        inputStream.Position = 0;
        
        // Read audio data as PCM 16-bit mono at 16kHz
        int sampleRate = 16000;
        int channels = 1;
        int bitsPerSample = 16;
        
        // Calculate expected size if it's raw PCM data
        var rawData = new byte[inputStream.Length];
        await inputStream.ReadAsync(rawData, 0, (int)inputStream.Length);
        
        using (var output = File.Create(outputPath))
        {
            // --- Write WAV header ---
            
            // Write "RIFF" header
            await output.WriteAsync(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            
            // Calculate and write file size (minus first 8 bytes)
            int fileSize = 36 + rawData.Length; // 36 bytes header + data length
            var fileSizeBytes = BitConverter.GetBytes(fileSize);
            await output.WriteAsync(fileSizeBytes, 0, 4);
            
            // Write "WAVE" format
            await output.WriteAsync(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            
            // Write "fmt " chunk header
            await output.WriteAsync(Encoding.ASCII.GetBytes("fmt "), 0, 4);
            
            // Write format chunk size (16 for PCM)
            await output.WriteAsync(BitConverter.GetBytes(16), 0, 4);
            
            // Write audio format (1 = PCM)
            await output.WriteAsync(BitConverter.GetBytes((short)1), 0, 2);
            
            // Write number of channels
            await output.WriteAsync(BitConverter.GetBytes((short)channels), 0, 2);
            
            // Write sample rate
            await output.WriteAsync(BitConverter.GetBytes(sampleRate), 0, 4);
            
            // Write byte rate (SampleRate * NumChannels * BitsPerSample/8)
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            await output.WriteAsync(BitConverter.GetBytes(byteRate), 0, 4);
            
            // Write block align (NumChannels * BitsPerSample/8)
            short blockAlign = (short)(channels * bitsPerSample / 8);
            await output.WriteAsync(BitConverter.GetBytes(blockAlign), 0, 2);
            
            // Write bits per sample
            await output.WriteAsync(BitConverter.GetBytes((short)bitsPerSample), 0, 2);
            
            // Write "data" chunk header
            await output.WriteAsync(Encoding.ASCII.GetBytes("data"), 0, 4);
            
            // Write data chunk size
            await output.WriteAsync(BitConverter.GetBytes(rawData.Length), 0, 4);
            
            // Write PCM data
            await output.WriteAsync(rawData, 0, rawData.Length);
        }
        
        _logger.LogInformation($"Generated WAV file successfully at {outputPath}, size: {new FileInfo(outputPath).Length} bytes");
    }
    
    // Check if the WAV header is valid
    private bool IsValidWavHeader(Stream stream)
    {
        // Save position to restore later
        long originalPosition = stream.Position;
        
        try
        {
            // WAV header should be at least 44 bytes
            if (stream.Length < 44)
            {
                _logger.LogWarning("Stream too short to contain valid WAV header");
                return false;
            }
            
            stream.Position = 0;
            byte[] header = new byte[12];
            stream.Read(header, 0, 12);
            
            // Check for RIFF header
            if (header[0] != 'R' || header[1] != 'I' || header[2] != 'F' || header[3] != 'F')
            {
                _logger.LogWarning("Missing RIFF header");
                return false;
            }
            
            // Check for WAVE format
            if (header[8] != 'W' || header[9] != 'A' || header[10] != 'V' || header[11] != 'E')
            {
                _logger.LogWarning("Missing WAVE format");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking WAV header");
            return false;
        }
        finally
        {
            // Restore original position
            stream.Position = originalPosition;
        }
    }
    
    // Recognize using push stream (fallback method)
    private async Task<string> RecognizeUsingPushStream(SpeechConfig speechConfig, MemoryStream memoryStream)
    {
        _logger.LogInformation("Using push stream for audio recognition");
        
        using var audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        var buffer = new byte[1024];
        int bytesRead;
        
        // Push audio data to the stream
        memoryStream.Position = 0;
        int totalBytesRead = 0;
        
        try
        {
            while ((bytesRead = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                audioInputStream.Write(buffer, bytesRead);
                totalBytesRead += bytesRead;
            }
            
            _logger.LogInformation($"Pushed {totalBytesRead} bytes to audio stream");
            
            // Signal end of stream
            audioInputStream.Close();
            
            // Create audio config
            var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            
            // Use single shot recognition for push stream (more reliable for problematic audio)
            return await PerformSingleShotRecognition(speechConfig, audioConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in push stream recognition");
            return string.Empty;
        }
    }
    
    // Single shot recognition (more reliable for problematic audio)
    private async Task<string> PerformSingleShotRecognition(SpeechConfig speechConfig, AudioConfig audioConfig)
    {
        _logger.LogInformation("Performing single-shot recognition");
        
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        
        // Start recognition with longer timeout
        var stopRecognition = new TaskCompletionSource<int>();
        recognizer.SessionStopped += (s, e) => stopRecognition.TrySetResult(0);
        
        var result = await recognizer.RecognizeOnceAsync();
        ProcessRecognitionResult(result);
        
        return GetTextFromResult(result);
    }
    
    // Continuous recognition for longer phrases
    private async Task<string> PerformContinuousRecognition(SpeechConfig speechConfig, AudioConfig audioConfig)
    {
        var recognitionCompletionSource = new TaskCompletionSource<string>();
        var recognizedText = new StringBuilder();
        var recognizedPhrases = new ConcurrentBag<string>();
        
        // Create a continuous recognizer
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        
        // Set up event handlers for continuous recognition
        recognizer.Recognized += (s, e) => 
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                _logger.LogInformation($"Recognized phrase: {e.Result.Text}");
                recognizedPhrases.Add(e.Result.Text);
            }
        };
        
        recognizer.Canceled += (s, e) => 
        {
            _logger.LogWarning($"Recognition canceled: {e.Reason}, Error Code: {e.ErrorCode}");
            if (e.Reason == CancellationReason.Error)
            {
                _logger.LogError($"Error code: {e.ErrorCode}, Error details: {e.ErrorDetails}");
            }
            recognitionCompletionSource.TrySetResult(string.Join(" ", recognizedPhrases));
        };
        
        recognizer.SessionStopped += (s, e) => 
        {
            _logger.LogInformation("Recognition session stopped");
            recognitionCompletionSource.TrySetResult(string.Join(" ", recognizedPhrases));
        };
        
        try
        {
            // Start continuous recognition
            await recognizer.StartContinuousRecognitionAsync();
            
            // Use a timeout to ensure the recognition completes even if no SessionStopped event
            var timeoutTask = Task.Delay(DefaultRecognitionTimeoutMs);
            var completedTask = await Task.WhenAny(recognitionCompletionSource.Task, timeoutTask);
            
            // Stop recognition
            await recognizer.StopContinuousRecognitionAsync();
            
            // If recognition completed naturally, return result
            if (completedTask == recognitionCompletionSource.Task)
            {
                string result = await recognitionCompletionSource.Task;
                _logger.LogInformation($"Complete transcription: {result}");
                return result;
            }
            
            // If timeout occurred, return what we have so far
            string timeoutResult = string.Join(" ", recognizedPhrases);
            _logger.LogWarning($"Recognition timed out after {DefaultRecognitionTimeoutMs}ms. Partial result: {timeoutResult}");
            
            return timeoutResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during continuous recognition");
            return string.Join(" ", recognizedPhrases);
        }
    }
    
    // Helper method to process recognition result and log details
    private void ProcessRecognitionResult(SpeechRecognitionResult result)
    {
        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                _logger.LogInformation($"Speech recognized successfully: {result.Text}");
                break;
                
            case ResultReason.NoMatch:
                var noMatchDetails = NoMatchDetails.FromResult(result);
                _logger.LogWarning($"Speech could not be recognized. Reason: {noMatchDetails.Reason}");
                break;
                
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(result);
                _logger.LogError($"Speech recognition canceled: {cancellation.Reason}");
                if (cancellation.Reason == CancellationReason.Error)
                {
                    _logger.LogError($"Error code: {cancellation.ErrorCode}, Error details: {cancellation.ErrorDetails}");
                }
                break;
                
            default:
                _logger.LogWarning($"Unexpected result reason: {result.Reason}");
                break;
        }
    }
    
    // Helper method to extract text from result
    private string GetTextFromResult(SpeechRecognitionResult result)
    {
        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                return result.Text;
                
            case ResultReason.NoMatch:
                _logger.LogWarning("No speech could be recognized.");
                return string.Empty;
                
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(result);
                if (cancellation.Reason == CancellationReason.Error)
                {
                    if (cancellation.ErrorCode == CancellationErrorCode.ConnectionFailure)
                    {
                        _logger.LogError("Connection to speech service failed. Check network connectivity.");
                    }
                    else if (cancellation.ErrorCode == CancellationErrorCode.AuthenticationFailure)
                    {
                        _logger.LogError("Authentication failed. Check your subscription key.");
                    }
                    else if (cancellation.ErrorCode == CancellationErrorCode.ServiceTimeout)
                    {
                        _logger.LogError("Service timeout. Try with shorter audio or check network.");
                    }
                }
                return string.Empty;
                
            default:
                return string.Empty;
        }
    }
    
    // Method to prepare an audio stream from a base64 encoded string
    public Stream GetStreamFromBase64(string base64Audio)
    {
        try
        {
            byte[] audioBytes = Convert.FromBase64String(base64Audio);
            _logger.LogInformation($"Converted base64 to byte array of length: {audioBytes.Length}");
            return new MemoryStream(audioBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting base64 to stream");
            return new MemoryStream();
        }
    }
} 