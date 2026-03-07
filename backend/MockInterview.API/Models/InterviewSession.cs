using System;

namespace MockInterview.API.Models
{
    public class InterviewSession
    {
        public Guid Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string VideoFilePath { get; set; } = string.Empty;
        public string TranscribedText { get; set; } = string.Empty;
        public string AiFeedback { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
