using System.Text;
using System.Text.Json;

namespace MockInterview.API.Services
{
    public class GeminiAiAnalysisService : IAiAnalysisService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public GeminiAiAnalysisService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<string> AnalyzeInterviewAsync(string videoPath)
        {
            var apiKey = _config["AiService:GeminiApiKey"];
            var model = _config["AiService:Model"] ?? "gemini-1.5-flash";
            
            if (string.IsNullOrEmpty(apiKey) || apiKey == "AIzaSyDLGbJtyKnAvDHkMu0tH4lsm5c2H5Gbwwo") // Using the key provided
            {
                 // Fallback if key isn't set yet
                 if (apiKey == "PASTE_YOUR_GEMINI_KEY_HERE")
                    return "Mock Analysis: Please provide a valid Gemini API Key in appsettings.json.";
            }

            try
            {
                // 1. Upload the video file to Gemini File API
                var fileInfo = await UploadFileToGemini(videoPath, apiKey);
                var fileUri = fileInfo.Uri;

                // 2. Wait for processing (Usually immediate for small files, but good practice for video)
                await PollingProcessState(fileInfo.Name, apiKey);

                // 3. Generate Content (Transcription + Feedback)
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var prompt = "Watch this interview video carefully. " +
                             "1. Transcribe the user's speech accurately. " +
                             "2. Provide expert-level feedback on their technical explanation, confidence, and body language. " +
                             "Format the output as follows:\n[TRANSCRIPT]\n(The text here)\n\n[FEEDBACK]\n(The analysis here)";

                var requestBody = new
                {
                    contents = new[] {
                        new {
                            parts = new object[] {
                                new { text = prompt },
                                new { file_data = new { mime_type = "video/webm", file_uri = fileUri } }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonResponse);
                    return doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "Analysis failed.";
                }

                return $"Final Analysis Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Multimodal Analysis Exception: {ex.Message}";
            }
        }

        private async Task<(string Uri, string Name)> UploadFileToGemini(string filePath, string apiKey)
        {
            var uploadUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";
            
            using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/webm");

            var request = new MultipartFormDataContent();
            var metadata = new { file = new { display_name = Path.GetFileName(filePath) } };
            
            request.Add(new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json"), "metadata");
            request.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync(uploadUrl, request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var fileProp = doc.RootElement.GetProperty("file");
            
            return (fileProp.GetProperty("uri").GetString()!, fileProp.GetProperty("name").GetString()!);
        }

        private async Task PollingProcessState(string fileName, string apiKey)
        {
            var statusUrl = $"https://generativelanguage.googleapis.com/v1beta/{fileName}?key={apiKey}";
            
            for (int i = 0; i < 10; i++) // Poll up to 10 times
            {
                var response = await _httpClient.GetAsync(statusUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var state = doc.RootElement.GetProperty("state").GetString();
                    
                    if (state == "ACTIVE") return;
                    if (state == "FAILED") throw new Exception("Gemini video processing failed.");
                }
                await Task.Delay(2000); // Wait 2 seconds between polls
            }
        }
    }
}
