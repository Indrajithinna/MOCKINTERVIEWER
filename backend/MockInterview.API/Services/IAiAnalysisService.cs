using System.Threading.Tasks;

namespace MockInterview.API.Services
{
    public interface IAiAnalysisService
    {
        Task<string> AnalyzeInterviewAsync(string videoPath);
    }
}
