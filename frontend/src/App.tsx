import { useState, useEffect } from 'react';
import axios from 'axios';
import WebcamRecorder from './components/WebcamRecorder';
import Timer from './components/Timer';
import FeedbackDisplay from './components/FeedbackDisplay';
import { PlayCircle, Loader2, Sparkles } from 'lucide-react';

const API_BASE_URL = 'http://localhost:5000/api/Interview';

type AppState = 'START' | 'INTERVIEW' | 'ANALYZING' | 'RESULT';

function App() {
    const [state, setState] = useState<AppState>('START');
    const [question, setQuestion] = useState('');
    const [feedback, setFeedback] = useState('');
    const [videoUrl, setVideoUrl] = useState('');
    const [error, setError] = useState('');

    const fetchQuestion = async () => {
        try {
            const response = await axios.get(`${API_BASE_URL}/questions`);
            setQuestion(response.data.question);
        } catch (err) {
            console.error('Failed to fetch question:', err);
            setQuestion('Explain Dependency Injection and its benefits.'); // Fallback
        }
    };

    const handleStart = async () => {
        await fetchQuestion();
        setState('INTERVIEW');
    };

    const handleRecordingComplete = async (videoBlob: Blob) => {
        setState('ANALYZING');

        const formData = new FormData();
        formData.append('video', videoBlob, 'interview.webm');

        try {
            const response = await axios.post(`${API_BASE_URL}/upload`, formData);
            setFeedback(response.data.aiFeedback);
            setVideoUrl(response.data.videoFilePath);
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

            {state === 'INTERVIEW' && (
                <div className="w-full max-w-4xl space-y-6">
                    <div className="bg-slate-900/50 backdrop-blur-xl border border-slate-800 p-6 rounded-2xl shadow-2xl">
                        <h2 className="text-indigo-400 font-semibold mb-2 uppercase text-sm tracking-widest">Question</h2>
                        <p className="text-2xl font-display font-medium leading-tight">{question}</p>
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
                        <h2 className="text-3xl font-bold mb-2">Analyzing Performance</h2>
                        <p className="text-gray-400">Our AI is evaluating your communication skills and technical accuracy...</p>
                    </div>
                </div>
            )}

            {state === 'RESULT' && (
                <>
                    {error && <div className="bg-red-500/10 border border-red-500 text-red-500 px-4 py-2 rounded mb-4">{error}</div>}
                    <FeedbackDisplay feedback={feedback} videoUrl={videoUrl} />
                </>
            )}
        </div>
    );
}

export default App;
