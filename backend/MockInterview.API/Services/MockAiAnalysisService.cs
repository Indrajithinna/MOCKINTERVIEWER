using System.Threading.Tasks;

namespace MockInterview.API.Services
{
    public class MockAiAnalysisService : IAiAnalysisService
    {
        private readonly IConfiguration _config;

        public MockAiAnalysisService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> AnalyzeInterviewAsync(string videoPath)
        {
            // Access your keys like this:
            var apiKey = _config["AiService:OpenAiApiKey"];
            
            // Simulate AI processing delay
            await Task.Delay(2000);
            
            return "Analysis Complete: You mentioned key terms like 'Polymorphism'. Good pacing, but try to reduce filler words.";
        }
    }
}
