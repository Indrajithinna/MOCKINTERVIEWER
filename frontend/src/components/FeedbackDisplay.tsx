import React from 'react';
import { Volume2, RefreshCcw } from 'lucide-react';

interface FeedbackProps {
    feedback: string;
    videoUrl?: string;
    onSpeak?: () => void;
}

const FeedbackDisplay: React.FC<FeedbackProps> = ({ feedback, videoUrl, onSpeak }) => {
    return (
        <div className="max-w-2xl w-full bg-slate-900 border border-slate-800 rounded-3xl shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-500">
            <div className="p-8">
                <div className="flex justify-between items-center mb-6">
                    <h2 className="text-3xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-indigo-400 to-purple-400 font-display">AI Performance Review</h2>
                    {onSpeak && (
                        <button
                            onClick={onSpeak}
                            className="p-3 bg-indigo-500/10 hover:bg-indigo-500/20 rounded-full transition-all group"
                        >
                            <Volume2 className="h-6 w-6 text-indigo-400 group-hover:scale-110 transition-transform" />
                        </button>
                    )}
                </div>

                {videoUrl && (
                    <div className="mb-8 rounded-2xl overflow-hidden shadow-2xl bg-black border border-slate-800 aspect-video group relative">
                        <video
                            src={`http://localhost:5000${videoUrl}`}
                            controls
                            className="w-full h-full"
                        />
                    </div>
                )}

                <div className="relative">
                    <div className="absolute -left-4 top-0 bottom-0 w-1 bg-gradient-to-b from-indigo-500 to-purple-600 rounded-full opacity-50"></div>
                    <div className="bg-slate-800/50 p-6 rounded-2xl border border-slate-700/50 backdrop-blur-sm">
                        <p className="text-gray-200 text-lg leading-relaxed italic">
                            "{feedback}"
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default FeedbackDisplay;
