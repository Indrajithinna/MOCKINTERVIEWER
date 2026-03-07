import { useState, useRef, useEffect } from 'react';
import axios from 'axios';
import WebcamRecorder from './components/WebcamRecorder';
import Timer from './components/Timer';
import FeedbackDisplay from './components/FeedbackDisplay';
import { PlayCircle, Loader2, Sparkles, Upload, FileText, CheckCircle2, Volume2 } from 'lucide-react';

const API_BASE_URL = 'http://localhost:5000/api/Interview';

type AppState = 'START' | 'RESUME_UPLOAD' | 'INTRO' | 'INTERVIEW' | 'ANALYZING' | 'RESULT';

function App() {
    const [state, setState] = useState<AppState>('START');
    const [question, setQuestion] = useState('');
    const [questionAudio, setQuestionAudio] = useState('');
    const [feedback, setFeedback] = useState('');
    const [feedbackAudio, setFeedbackAudio] = useState('');
    const [videoUrl, setVideoUrl] = useState('');
    const [error, setError] = useState('');
    const [resumePath, setResumePath] = useState<string | null>(null);
    const [language, setLanguage] = useState('en-IN');
    const [isUploadingResume, setIsUploadingResume] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const LANGUAGES = [
        { code: 'en-IN', name: 'English' },
        { code: 'hi-IN', name: 'Hindi' },
        { code: 'kn-IN', name: 'Kannada' },
        { code: 'te-IN', name: 'Telugu' },
        { code: 'ml-IN', name: 'Malayalam' }
    ];

    // Speak function (supports both native TTS and Sarvam Audio files)
    const speak = (text: string, audioUrl?: string) => {
        if (audioUrl) {
            const audio = new Audio(`${API_BASE_URL.replace('/api/Interview', '')}${audioUrl}`);
            audio.play().catch(e => console.error("Audio play failed:", e));
            return;
        }

        if ('speechSynthesis' in window) {
            window.speechSynthesis.cancel();
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.rate = 0.95;
            utterance.pitch = 1.05;
            window.speechSynthesis.speak(utterance);
        }
    };

    // Automatically speak when state changes
    useEffect(() => {
        if (state === 'INTRO') {
            speak("Hello! I am your A I Mock Interviewer. I have prepared a few questions for you. Are you ready to begin?");
        } else if (state === 'INTERVIEW' && question) {
            speak(question, questionAudio);
        }
    }, [question, questionAudio, state]);

    const fetchQuestion = async () => {
        try {
            if (resumePath) {
                // To keep this demo simple, if they upload a resume, we just fetch a random question for now 
                // in the real implementation this would call resume-questions and also hit the TTS endpoint.
                // We'll fallback to standard TTS if audio isn't returned for resume questions.
                const response = await axios.post(`${API_BASE_URL}/resume-questions?resumeFilePath=${encodeURIComponent(resumePath)}&language=${language}`);
                const questions = JSON.parse(response.data.questions);
                setQuestion(questions[0] || "Tell me about yourself.");
            } else {
                const response = await axios.get(`${API_BASE_URL}/questions?language=${language}`);
                setQuestion(response.data.question);
                if (response.data.audioUrl) {
                    setQuestionAudio(response.data.audioUrl);
                }
            }
        } catch (err) {
            console.error('Failed to fetch question:', err);
            setQuestion('Explain Dependency Injection and its benefits.');
        }
    };

    const handleStart = () => {
        setState('RESUME_UPLOAD');
    };

    const handleResumeUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        setIsUploadingResume(true);
        const formData = new FormData();
        formData.append('resume', file);

        try {
            const response = await axios.post(`${API_BASE_URL}/upload-resume`, formData);
            setResumePath(response.data.resumeFilePath);
        } catch (err) {
            console.error('Resume upload failed:', err);
            setError('Failed to upload resume. Proceeding without it.');
        } finally {
            setIsUploadingResume(false);
        }
    };

    const proceedToInterview = async () => {
        setState('ANALYZING'); // Show loading while fetching question
        await fetchQuestion();
        setState('INTRO');
    };

    const handleRecordingComplete = async (videoBlob: Blob) => {
        setState('ANALYZING');

        const formData = new FormData();
        formData.append('video', videoBlob, 'interview.webm');

        try {
            const url = resumePath
                ? `${API_BASE_URL}/upload?resumeFilePath=${encodeURIComponent(resumePath)}&language=${language}`
                : `${API_BASE_URL}/upload?language=${language}`;

            const response = await axios.post(url, formData);

            // Expected response logic updated to handle audioUrl
            if (response.data.session) {
                setFeedback(response.data.session.aiFeedback);
                setVideoUrl(response.data.session.videoFilePath);
            } else {
                setFeedback(response.data.aiFeedback);
                setVideoUrl(response.data.videoFilePath);
            }

            if (response.data.feedbackAudioUrl) {
                setFeedbackAudio(response.data.feedbackAudioUrl);
            }

            setState('RESULT');
        } catch (err) {
            console.error('Upload failed:', err);
            setError('Communication with AI server failed. Displaying mock results.');
            setFeedback("Analysis Complete: You mentioned key terms like 'Polymorphism'. Good pacing, but try to reduce filler words.");
            setState('RESULT');
        }
    };

    return (
        <div className="min-h-screen bg-slate-950 text-white flex flex-col items-center justify-center p-4">
            <style>{`
                @keyframes pulse-ring {
                    0% { transform: scale(0.33); opacity: 1; }
                    80%, 100% { opacity: 0; }
                }
                .voice-active::before {
                    content: '';
                    position: absolute;
                    width: 100%;
                    height: 100%;
                    border: 2px solid #6366f1;
                    border-radius: 50%;
                    animation: pulse-ring 1.25s cubic-bezier(0.215, 0.61, 0.355, 1) infinite;
                }
            `}</style>

            {state === 'START' && (
                <div className="max-w-md w-full text-center space-y-8 animate-in fade-in zoom-in duration-500">
                    <div className="relative inline-block">
                        <div className="absolute -inset-1 bg-gradient-to-r from-indigo-500 to-purple-600 rounded-full blur opacity-75 group-hover:opacity-100 transition duration-1000 group-hover:duration-200"></div>
                        <Sparkles className="relative h-20 w-20 text-indigo-400 mx-auto" />
                    </div>
                    <h1 className="text-5xl font-extrabold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-indigo-400 to-purple-400">
                        Mock Interview AI
                    </h1>
                    <p className="text-gray-400 text-lg">
                        Practice technical interviews with real-time AI feedback and video analysis.
                    </p>
                    <button
                        onClick={handleStart}
                        className="group relative inline-flex items-center justify-center px-8 py-4 font-bold text-white transition-all duration-200 bg-indigo-600 font-pj rounded-xl focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-600 hover:bg-indigo-700 w-full"
                    >
                        <PlayCircle className="w-6 h-6 mr-2" />
                        Start Mock Interview
                    </button>
                </div>
            )}

            {state === 'RESUME_UPLOAD' && (
                <div className="max-w-lg w-full bg-slate-900/50 backdrop-blur-xl border border-slate-800 p-8 rounded-3xl shadow-2xl space-y-8 animate-in fade-in slide-in-from-bottom-8 duration-500">
                    <div className="text-center">
                        <h2 className="text-3xl font-bold mb-2">Upload Your Resume</h2>
                        <p className="text-gray-400">Our AI will tailor the interview questions based on your experience and skills.</p>
                    </div>

                    <div
                        onClick={() => fileInputRef.current?.click()}
                        className={`border-2 border-dashed rounded-2xl p-10 text-center cursor-pointer transition-all duration-200 ${resumePath ? 'border-emerald-500/50 bg-emerald-500/5' : 'border-slate-700 hover:border-indigo-500 hover:bg-indigo-500/5'}`}
                    >
                        <input
                            type="file"
                            ref={fileInputRef}
                            onChange={handleResumeUpload}
                            accept=".pdf,.txt,.doc,.docx"
                            className="hidden"
                        />
                        {resumePath ? (
                            <div className="flex flex-col items-center space-y-3">
                                <CheckCircle2 className="h-12 w-12 text-emerald-500" />
                                <span className="text-emerald-400 font-medium">Resume Uploaded Successfully!</span>
                            </div>
                        ) : isUploadingResume ? (
                            <div className="flex flex-col items-center space-y-3">
                                <Loader2 className="h-12 w-12 text-indigo-500 animate-spin" />
                                <span className="text-gray-400">Analyzing Resume...</span>
                            </div>
                        ) : (
                            <div className="flex flex-col items-center space-y-3">
                                <Upload className="h-12 w-12 text-slate-500" />
                                <span className="text-slate-400">Click to upload or drag and drop</span>
                                <span className="text-xs text-slate-600">PDF, TXT, DOCX up to 10MB</span>
                            </div>
                        )}
                    </div>

                    <div className="flex flex-col space-y-3">
                        <label className="text-gray-400 font-medium text-left">Select Interview Language</label>
                        <select
                            value={language}
                            onChange={(e) => setLanguage(e.target.value)}
                            className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-3 text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all cursor-pointer"
                        >
                            {LANGUAGES.map((lang) => (
                                <option key={lang.code} value={lang.code}>{lang.name}</option>
                            ))}
                        </select>
                    </div>

                    <div className="flex flex-col space-y-3">
                        <button
                            onClick={proceedToInterview}
                            className="w-full py-4 bg-indigo-600 hover:bg-indigo-700 rounded-xl font-bold transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {resumePath ? 'Get Started with Tailored Questions' : 'Skip & Use General Questions'}
                        </button>
                    </div>
                </div>
            )}

            {state === 'INTRO' && (
                <div className="max-w-lg w-full bg-slate-900/50 backdrop-blur-xl border border-slate-800 p-10 rounded-3xl shadow-2xl space-y-8 animate-in fade-in zoom-in duration-500 text-center">
                    <div className="mx-auto w-24 h-24 rounded-full flex items-center justify-center mb-6 relative">
                        <div className="absolute inset-0 bg-indigo-500 rounded-full animate-ping opacity-20"></div>
                        <div className="relative bg-indigo-500/20 w-full h-full rounded-full flex items-center justify-center">
                            <Sparkles className="h-10 w-10 text-indigo-400" />
                        </div>
                    </div>
                    <h2 className="text-4xl font-bold mb-4 text-white font-display">Are you ready?</h2>
                    <p className="text-gray-300 text-lg leading-relaxed">
                        I am your AI Mock Interviewer. I have prepared your interview questions. Let me know when you're ready to start.
                    </p>
                    <button
                        onClick={() => setState('INTERVIEW')}
                        className="w-full py-4 mt-8 bg-gradient-to-r from-emerald-500 to-emerald-600 hover:from-emerald-600 hover:to-emerald-700 text-white rounded-xl font-bold text-lg shadow-lg hover:shadow-emerald-500/25 transition-all transform hover:scale-[1.02] flex items-center justify-center gap-2"
                    >
                        Yes, I'm Ready!
                    </button>
                    <div className="absolute top-4 right-4">
                        <button onClick={() => speak("Hello! I am your A I Mock Interviewer. I have prepared a few questions for you. Are you ready to begin?")} className="p-2 bg-indigo-500/10 hover:bg-indigo-500/20 rounded-full transition-colors">
                            <Volume2 className="h-5 w-5 text-indigo-400" />
                        </button>
                    </div>
                </div>
            )}

            {state === 'INTERVIEW' && (
                <div className="w-full max-w-4xl space-y-6">
                    <div className="bg-slate-900/50 backdrop-blur-xl border border-slate-800 p-8 rounded-2xl shadow-2xl relative overflow-hidden group">
                        <div className="absolute top-4 right-4 animate-bounce">
                            <button onClick={() => speak(question, questionAudio)} className="p-2 bg-indigo-500/10 hover:bg-indigo-500/20 rounded-full transition-colors">
                                <Volume2 className="h-5 w-5 text-indigo-400" />
                            </button>
                        </div>
                        <h2 className="text-indigo-400 font-semibold mb-3 uppercase text-sm tracking-widest flex items-center gap-2">
                            {resumePath && <FileText className="w-4 h-4" />}
                            {resumePath ? 'Tailored Question' : 'General Question'}
                        </h2>
                        <p className="text-3xl font-display font-medium leading-tight text-white pr-10">{question}</p>
                    </div>

                    <WebcamRecorder
                        isRecording={true}
                        onRecordingComplete={handleRecordingComplete}
                    />

                    <div className="flex justify-center">
                        <Timer
                            initialSeconds={120}
                            onTimeUp={() => { }}
                            isActive={true}
                        />
                    </div>
                </div>
            )}

            {state === 'ANALYZING' && (
                <div className="text-center space-y-6">
                    <Loader2 className="h-16 w-16 text-indigo-500 animate-spin mx-auto" />
                    <div>
                        <h2 className="text-3xl font-bold mb-2">AI is Thinking...</h2>
                        <p className="text-gray-400">Analyzing your response and resume context...</p>
                    </div>
                </div>
            )}

            {state === 'RESULT' && (
                <>
                    {error && <div className="bg-red-500/10 border border-red-500 text-red-500 px-4 py-2 rounded mb-4">{error}</div>}
                    <FeedbackDisplay feedback={feedback} videoUrl={videoUrl} onSpeak={() => speak(feedback, feedbackAudio)} />
                    <button
                        onClick={() => window.location.reload()}
                        className="mt-8 px-6 py-2 bg-slate-800 hover:bg-slate-700 rounded-lg transition-colors border border-slate-700"
                    >
                        Try Another Interview
                    </button>
                </>
            )}
        </div>
    );
}

export default App;
