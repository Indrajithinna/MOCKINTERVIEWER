import React from 'react';

interface FeedbackProps {
    feedback: string;
    videoUrl?: string;
}

const FeedbackDisplay: React.FC<FeedbackProps> = ({ feedback, videoUrl }) => {
    return (
        <div className="max-w-2xl w-full bg-white dark:bg-gray-800 rounded-xl shadow-2xl overflow-hidden border border-gray-200 dark:border-gray-700">
            <div className="p-8">
                <h2 className="text-3xl font-bold text-indigo-600 dark:text-indigo-400 mb-6 font-display">AI Interview Feedback</h2>

                {videoUrl && (
                    <div className="mb-8 rounded-lg overflow-hidden shadow-lg bg-black aspect-video">
                        <video
                            src={`http://localhost:5000${videoUrl}`}
                            controls
                            className="w-full h-full"
                        />
                    </div>
                )}

                <div className="bg-indigo-50 dark:bg-indigo-900/30 p-6 rounded-lg border-l-4 border-indigo-500">
                    <p className="text-gray-800 dark:text-gray-200 leading-relaxed italic">
                        "{feedback}"
                    </p>
                </div>

                <button
                    onClick={() => window.location.reload()}
                    className="mt-8 w-full bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-3 px-6 rounded-lg transition duration-200 ease-in-out transform hover:scale-[1.02]"
                >
                    Try Another Question
                </button>
            </div>
        </div>
    );
};

export default FeedbackDisplay;
