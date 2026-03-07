using Microsoft.AspNetCore.Mvc;
using MockInterview.API.Models;
using MockInterview.API.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MockInterview.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly IAiAnalysisService _aiService;
        private readonly ITtsService _ttsService;
        private readonly IWebHostEnvironment _env;

        private static readonly List<string> Questions = new()
        {
            "Explain Dependency Injection and its benefits.",
            "What is the difference between an Abstract Class and an Interface?",
            "How do you handle error management in a Web API?",
            "What are Solid principles? Explain one of them.",
            "Explain Polymorphism with a real-world example."
        };

        public InterviewController(IAiAnalysisService aiService, ITtsService ttsService, IWebHostEnvironment env)
        {
            _aiService = aiService;
            _ttsService = ttsService;
            _env = env;
        }

        [HttpGet("questions")]
        public async Task<ActionResult<string>> GetRandomQuestion([FromQuery] string language = "en-IN")
        {
            var random = new Random();
            int index = random.Next(Questions.Count);
            var q = Questions[index];
            var audioUrl = await _ttsService.GenerateSpeechAsync(q, language);
            return Ok(new { question = q, audioUrl = audioUrl });
        }

        [HttpPost("upload-resume")]
        public async Task<IActionResult> UploadResume(IFormFile resume)
        {
            if (resume == null || resume.Length == 0)
                return BadRequest("No resume file uploaded.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "resumes");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(resume.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resume.CopyToAsync(stream);
            }

            return Ok(new { resumeFilePath = filePath });
        }

        [HttpPost("resume-questions")]
        public async Task<IActionResult> GetResumeQuestions([FromQuery] string resumeFilePath, [FromQuery] string language = "en-IN")
        {
            if (string.IsNullOrEmpty(resumeFilePath) || !System.IO.File.Exists(resumeFilePath))
                return BadRequest("Invalid resume file path.");

            var questionsJson = await _aiService.GenerateQuestionsFromResumeAsync(resumeFilePath, language);
            var firstQuestion = "Tell me about yourself based on your resume.";

            try 
            {
                // Robust extraction of the first string from a JSON array using Regex
                var match = Regex.Match(questionsJson, @"\[\s*""([^""]+)""");
                if (match.Success) 
                {
                    firstQuestion = match.Groups[1].Value;
                }
            } 
            catch { }

            var audioUrl = await _ttsService.GenerateSpeechAsync(firstQuestion, language);

            return Ok(new { question = firstQuestion, audioUrl = audioUrl });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo(IFormFile video, [FromQuery] string? resumeFilePath = null, [FromQuery] string language = "en-IN")
        {
            if (video == null || video.Length == 0)
                return BadRequest("No video file uploaded.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(video.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await video.CopyToAsync(stream);
            }

            var feedback = await _aiService.AnalyzeInterviewAsync(filePath, resumeFilePath, language);

            var session = new InterviewSession
            {
                Id = Guid.NewGuid(),
                QuestionText = "User-selected question", // In a real app, this would be passed or tracked
                VideoFilePath = $"/uploads/{fileName}",
                AiFeedback = feedback,
                CreatedAt = DateTime.UtcNow
            };

            var feedbackAudioUrl = await _ttsService.GenerateSpeechAsync(feedback, language);

            return Ok(new { session, feedbackAudioUrl });
        }
    }
}
