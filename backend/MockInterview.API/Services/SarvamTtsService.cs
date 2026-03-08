using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace MockInterview.API.Services
{
    public interface ITtsService
    {
        Task<string> GenerateSpeechAsync(string text, string languageCode = "en-IN");
    }

    public class SarvamTtsService : ITtsService
    {
        private readonly IConfiguration _config;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly Microsoft.Extensions.Logging.ILogger<SarvamTtsService> _logger;

        public SarvamTtsService(
            IConfiguration config, 
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            Microsoft.Extensions.Logging.ILogger<SarvamTtsService> logger)
        {
            _config = config;
            _env = env;
            _logger = logger;
        }

        public async Task<string> GenerateSpeechAsync(string text, string languageCode = "en-IN")
        {
            var apiKey = Environment.GetEnvironmentVariable("SARVAM_API_KEY") ?? _config["AiService:SarvamApiKey"];
            
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_SARVAM_KEY_HERE")
            {
                _logger.LogWarning("Sarvam API Key is missing or default.");
                return "";
            }

            var options = new RestClientOptions("https://api.sarvam.ai/text-to-speech");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("api-subscription-key", apiKey);
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                inputs = new[] { text },
                target_language_code = languageCode,
                speaker = "meera",
                pitch = 0,
                pace = 1.0,
                loudness = 1.5,
                speech_sample_rate = 8000,
                enable_preprocessing = true,
                model = "builtin/speech"
            };

            request.AddJsonBody(body);

            try
            {
                var response = await client.PostAsync(request);
                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    using var doc = JsonDocument.Parse(response.Content);
                    if (doc.RootElement.TryGetProperty("audios", out var audios) && audios.GetArrayLength() > 0)
                    {
                        var base64Audio = audios[0].GetString();

                        if (!string.IsNullOrEmpty(base64Audio))
                        {
                            var audioBytes = Convert.FromBase64String(base64Audio);

                            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "audio");
                            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                            var fileName = $"{Guid.NewGuid()}.wav";
                            var filePath = Path.Combine(uploadsFolder, fileName);
                            
                            await File.WriteAllBytesAsync(filePath, audioBytes);
                            
                            return $"/uploads/audio/{fileName}";
                        }
                    }
                }
                else
                {
                    _logger.LogError("Sarvam API request failed. Status: {Status}, Content: {Content}", response.StatusCode, response.Content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sarvam API parsing error");
            }

            return "";
        }
    }
}
