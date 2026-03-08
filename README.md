
This is a prototype for an AI-powered Mock Interview Platform.

## Project Structure
- **/backend**: ASP.NET Core Web API (.NET 8/9)
- **/frontend**: React (Vite + TypeScript + Tailwind CSS)

## Screenshots
*(Add screenshots here showing the interview process, feedback display, and multi-language support)*

## Prerequisites
- .NET 8 or 9 SDK
- Node.js (v18+) & NPM
- [Google Gemini API Key](https://aistudio.google.com/app/apikey)
- [Sarvam AI API Key](https://www.sarvam.ai/) (For TTS)

## Configuration

The application requires API keys for AI services. You can set them in `appsettings.json` or as environment variables:

- `AiService:GeminiApiKey` or `GEMINI_API_KEY`
- `AiService:SarvamApiKey` or `SARVAM_API_KEY`

## Supported Languages
The platform supports mock interviews in multiple Indian languages:
- English (en-IN)
- Hindi (hi-IN)
- Kannada (kn-IN)
- Telugu (te-IN)
- Malayalam (ml-IN)

## How to Run

### 1. Backend
1. Open `backend/MockInterview.API/appsettings.json`.
2. Replace `PASTE_YOUR_GEMINI_KEY_HERE` with your key from [Google AI Studio](https://aistudio.google.com/app/apikey).
3. Open a terminal in `backend/MockInterview.API`.
4. Run `dotnet run`.
5. The API will be available at `http://localhost:5000`.

### 2. Frontend
1. Open a terminal in `frontend`.
2. Run `npm install`.
3. Run `npm run dev`.
4. Open `http://localhost:5173` in your browser.

## Features
- **Random Technical Questions**: Fetches technical questions from the backend.
- **Webcam Recording**: Captures user video responses using the MediaStream API.
- **Timed Sessions**: Automatic 2-minute limit for responses.
- **AI Analysis (Mock)**: Simulates feedback generation based on the recorded session.
- **Video Playback**: Replays the recorded interview alongside the feedback.
