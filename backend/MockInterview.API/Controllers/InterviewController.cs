using Microsoft.AspNetCore.Mvc;
using MockInterview.API.Models;
using MockInterview.API.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MockInterview.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly IAiAnalysisService _aiService;
        private readonly IWebHostEnvironment _env;

        private static readonly List<string> Questions = new()
        {
            "Explain Dependency Injection and its benefits.",
            "What is the difference between an Abstract Class and an Interface?",
            "How do you handle error management in a Web API?",
            "What are Solid principles? Explain one of them.",
            "Explain Polymorphism with a real-world example."
        };

        public InterviewController(IAiAnalysisService aiService, IWebHostEnvironment env)
        {
            _aiService = aiService;
            _env = env;
        }

        [HttpGet("questions")]
        public ActionResult<string> GetRandomQuestion()
        {
            var random = new Random();
            int index = random.Next(Questions.Count);
            return Ok(new { question = Questions[index] });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo(IFormFile video)
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

            var feedback = await _aiService.AnalyzeInterviewAsync(filePath);

            var session = new InterviewSession
            {
                Id = Guid.NewGuid(),
                QuestionText = "User-selected question", // In a real app, this would be passed or tracked
                VideoFilePath = $"/uploads/{fileName}",
                AiFeedback = feedback,
                CreatedAt = DateTime.UtcNow
            };

            return Ok(session);
        }
    }
}
