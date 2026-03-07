import React, { useRef, useState, useCallback, useEffect } from 'react';

interface WebcamRecorderProps {
    onRecordingComplete: (blob: Blob) => void;
    isRecording: boolean;
}

const WebcamRecorder: React.FC<WebcamRecorderProps> = ({ onRecordingComplete, isRecording }) => {
    const videoRef = useRef<HTMLVideoElement>(null);
    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const [chunks, setChunks] = useState<Blob[]>([]);
    const [stream, setStream] = useState<MediaStream | null>(null);

    const startStream = useCallback(async () => {
        try {
            const userStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
            setStream(userStream);
            if (videoRef.current) {
                videoRef.current.srcObject = userStream;
            }
        } catch (err) {
            console.error("Error accessing webcam:", err);
        }
    }, []);

    const stopStream = useCallback(() => {
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            setStream(null);
        }
    }, [stream]);

    useEffect(() => {
        startStream();
        return () => stopStream();
    }, []);

    useEffect(() => {
        if (isRecording && stream) {
            const options = { mimeType: 'video/webm;codecs=vp8,opus' };
            const recorder = new MediaRecorder(stream, options);

            recorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    setChunks((prev) => [...prev, event.data]);
                }
            };

            recorder.onstop = () => {
                const blob = new Blob(chunks, { type: 'video/webm' });
                onRecordingComplete(blob);
                setChunks([]);
            };

            recorder.start();
            mediaRecorderRef.current = recorder;
        } else if (!isRecording && mediaRecorderRef.current && mediaRecorderRef.current.state === 'recording') {
            mediaRecorderRef.current.stop();
        }
    }, [isRecording, stream, chunks, onRecordingComplete]);

    return (
        <div className="relative w-full aspect-video bg-gray-900 rounded-2xl overflow-hidden shadow-2xl border-4 border-indigo-500/30">
            <video
                ref={videoRef}
                autoPlay
                muted
                playsInline
                className="w-full h-full object-cover mirror"
            />
            {isRecording && (
                <div className="absolute top-4 right-4 flex items-center space-x-2 bg-red-600 text-white px-3 py-1 rounded-full animate-pulse shadow-lg">
                    <div className="w-3 h-3 bg-white rounded-full"></div>
                    <span className="text-xs font-bold uppercase tracking-wider">Recording</span>
                </div>
            )}
        </div>
    );
};

export default WebcamRecorder;
