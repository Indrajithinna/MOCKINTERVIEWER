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

        private static readonly Dictionary<string, List<string>> TranslatedQuestions = new()
        {
            { "en-IN", new() { "Explain Dependency Injection and its benefits.", "What is the difference between an Abstract Class and an Interface?", "How do you handle error management in a Web API?", "What are Solid principles? Explain one of them.", "Explain Polymorphism with a real-world example." } },
            { "hi-IN", new() { "डिपेंडेंसी इंजेक्शन और इसके लाभों की व्याख्या करें।", "एब्स्ट्रेक्ट क्लास और इंटरफेस में क्या अंतर है?", "आप वेब एपीआई में त्रुटि प्रबंधन (error management) को कैसे सँभालते हैं?", "सॉलिड (SOLID) सिद्धांत क्या हैं? उनमें से एक की व्याख्या करें।", "वास्तविक दुनिया के उदाहरण के साथ बहुरूपता (Polymorphism) की व्याख्या करें।" } },
            { "kn-IN", new() { "ಡಿಪೆಂಡೆನ್ಸಿ ಇಂಜೆಕ್ಷನ್ ಮತ್ತು ಅದರ ಉಪಯೋಗಗಳನ್ನು ವಿವರಿಸಿ.", "ಅಮೂರ್ತ ವರ್ಗ (Abstract Class) ಮತ್ತು ಇಂಟರ್ಫೇಸ್ ನಡುವಿನ ವ್ಯತ್ಯಾಸವೇನು?", "ವೆಬ್ API ಯಲ್ಲಿ ದೋಷ ನಿರ್ವಹಣೆಯನ್ನು (error management) ಹೇಗೆ ನಿಭಾಯಿಸುತ್ತೀರಿ?", "ಸಾಲಿಡ್ (SOLID) ತತ್ವಗಳೆಂದರೇನು? ಅವುಗಳಲ್ಲಿ ಒಂದನ್ನು ವಿವರಿಸಿ.", "ನೈಜ ಪ್ರಪಂಚದ ಉದಾಹರಣೆಯೊಂದಿಗೆ ಪಾಲಿಮಾರ್ಫಿಸಂ (Polymorphism) ವಿವರಿಸಿ." } },
            { "te-IN", new() { "డిపెండెన్సీ ఇంజెక్షన్ మరియు దాని ప్రయోజనాలను వివరించండి.", "అబ్‌స్ట్రాక్ట్ క్లాస్ మరియు ఇంటర్‌ఫేస్ మధ్య తేడా ఏమిటి?", "వెబ్ API లో ఎర్రర్ మేనేజ్‌మెంట్‌ను ఎలా నిర్వహిస్తారు?", "సాలిడ్ (SOLID) సూత్రాలు ఏమిటి? వాటిలో ఒకదాన్ని వివరించండి.", "నిజ జీవిత ఉదాహరణతో పాలిమార్ఫిజం (Polymorphism) ను వివరించండి." } },
            { "ml-IN", new() { "ഡിപൻഡൻസി ഇൻജക്ഷനും അതിന്റെ പ്രയോജനങ്ങളും വിശദീകരിക്കുക.", "ഒരു അബ്സ്ട്രാക്ട് ക്ലാസ്സും ഇന്റർഫേസും തമ്മിലുള്ള വ്യത്യാസം എന്താണ്?", "വെബ് എപിഐ-ൽ നിങ്ങൾ എറർ മാനേജ്മെന്റ് എങ്ങനെ കൈകാര്യം ചെയ്യുന്നു?", "സോളിഡ് (SOLID) തത്വങ്ങൾ എന്തൊക്കെയാണ്? ഒരെണ്ണം വിശദീകരിക്കുക.", "ഒരു യഥാർത്ഥ ലോക ഉദാഹരണത്തോടെ പോളിമോർഫിസം (Polymorphism) വിശദീകരിക്കുക." } }
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
            var langKey = TranslatedQuestions.ContainsKey(language) ? language : "en-IN";
            var questions = TranslatedQuestions[langKey];
            
            var random = new Random();
            int index = random.Next(questions.Count);
            var q = questions[index];
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
