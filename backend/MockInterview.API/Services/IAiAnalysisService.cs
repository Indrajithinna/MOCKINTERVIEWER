using System.Threading.Tasks;

namespace MockInterview.API.Services
{
    public interface IAiAnalysisService
    {
        Task<string> AnalyzeInterviewAsync(string videoPath, string? resumeFilePath = null, string languageCode = "en-IN");
        Task<string> GenerateQuestionsFromResumeAsync(string resumeFilePath, string languageCode = "en-IN");
    }
}
