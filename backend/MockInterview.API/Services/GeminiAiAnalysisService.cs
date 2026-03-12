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

        public async Task<string> AnalyzeInterviewAsync(string videoPath, string? resumeFilePath = null, string languageCode = "en-IN")
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _config["AiService:GeminiApiKey"];
            var model = _config["AiService:Model"] ?? "gemini-1.5-flash";
            
            if (string.IsNullOrEmpty(apiKey) || apiKey == "AIzaSyDLGbJtyKnAvDHkMu0tH4lsm5c2H5Gbwwo" || apiKey == "PASTE_YOUR_GEMINI_KEY_HERE") 
            {
                return "Mock Analysis: Please provide a valid Gemini API Key in the backend .env file.";
            }

            var langMap = new Dictionary<string, string> {
                {"en-IN", "English"}, {"hi-IN", "Hindi"}, {"kn-IN", "Kannada"}, {"te-IN", "Telugu"}, {"ml-IN", "Malayalam"}
            };
            var langDesc = langMap.ContainsKey(languageCode) ? langMap[languageCode] : "English";

            try
            {
                // 1. Upload video
                var videoFileInfo = await UploadFileToGemini(videoPath, apiKey, "video/webm");
                var videoUri = videoFileInfo.Uri;

                // 2. Upload resume if available
                string? resumeUri = null;
                if (!string.IsNullOrEmpty(resumeFilePath))
                {
                    var ext = Path.GetExtension(resumeFilePath).ToLower();
                    var mime = ext == ".pdf" ? "application/pdf" : "text/plain";
                    var resumeFileInfo = await UploadFileToGemini(resumeFilePath, apiKey, mime);
                    resumeUri = resumeFileInfo.Uri;
                    await PollingProcessState(resumeFileInfo.Name, apiKey);
                }

                await PollingProcessState(videoFileInfo.Name, apiKey);

                // 3. Generate Content
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var prompt = $"Watch this interview video carefully. " +
                             (resumeUri != null ? "The user's resume is provided. " : "") +
                             $"1. Transcribe the user's speech accurately. " +
                             $"2. Provide expert-level feedback on their technical explanation, confidence, and body language." +
                             (resumeUri != null ? " Compare their answers with the skills and experience mentioned in their resume." : "") +
                             $"\nIMPORTANT: Provide your FULL analysis and transcript strictly in the {langDesc} language." +
                             "\nFormat the output exactly as follows:\n[TRANSCRIPT]\n(The text here)\n\n[FEEDBACK]\n(The analysis here)";

                var parts = new List<object>
                {
                    new { text = prompt },
                    new { file_data = new { mime_type = "video/webm", file_uri = videoUri } }
                };

                if (resumeUri != null)
                {
                    var mime = Path.GetExtension(resumeFilePath!).ToLower() == ".pdf" ? "application/pdf" : "text/plain";
                    parts.Add(new { file_data = new { mime_type = mime, file_uri = resumeUri } });
                }

                var requestBody = new
                {
                    contents = new[] {
                        new {
                            parts = parts.ToArray()
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

        private async Task<(string Uri, string Name)> UploadFileToGemini(string filePath, string apiKey, string mimeType)
        {
            var uploadUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";
            
            using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

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

        public async Task<string> GenerateQuestionsFromResumeAsync(string resumeFilePath, string languageCode = "en-IN")
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _config["AiService:GeminiApiKey"];
            var model = _config["AiService:Model"] ?? "gemini-1.5-flash";

            var langMap = new Dictionary<string, string> {
                {"en-IN", "English"}, {"hi-IN", "Hindi"}, {"kn-IN", "Kannada"}, {"te-IN", "Telugu"}, {"ml-IN", "Malayalam"}
            };
            var langDesc = langMap.ContainsKey(languageCode) ? langMap[languageCode] : "English";

            try
            {
                var ext = Path.GetExtension(resumeFilePath).ToLower();
                var mime = ext == ".pdf" ? "application/pdf" : "text/plain";
                var resumeFileInfo = await UploadFileToGemini(resumeFilePath, apiKey, mime);
                await PollingProcessState(resumeFileInfo.Name, apiKey);

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var prompt = $"Based on the provided resume, generate 5 relevant technical interview questions that would be challenging for this candidate. " +
                             $"Focus on the projects, skills, and experience mentioned. " +
                             $"IMPORTANT: The questions MUST be in the {langDesc} language. " +
                             $"Return ONLY the questions as a JSON array of strings.";

                var requestBody = new
                {
                    contents = new[] {
                        new {
                            parts = new object[] {
                                new { text = prompt },
                                new { file_data = new { mime_type = mime, file_uri = resumeFileInfo.Uri } }
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
                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    // Clean up markdown markers if present
                    if (text != null && text.Contains("```json"))
                    {
                        text = text.Replace("```json", "").Replace("```", "").Trim();
                    }

                    return text ?? "[]";
                }

                return "[]";
            }
            catch (Exception)
            {
                return "[]";
            }
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
