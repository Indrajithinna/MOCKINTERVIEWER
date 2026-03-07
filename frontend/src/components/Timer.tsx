import React, { useState, useEffect } from 'react';

interface TimerProps {
    initialSeconds: number;
    onTimeUp: () => void;
    isActive: boolean;
}

const Timer: React.FC<TimerProps> = ({ initialSeconds, onTimeUp, isActive }) => {
    const [seconds, setSeconds] = useState(initialSeconds);

    useEffect(() => {
        let interval: number | undefined;

        if (isActive && seconds > 0) {
            interval = setInterval(() => {
                setSeconds((prev) => prev - 1);
            }, 1000);
        } else if (seconds === 0) {
            onTimeUp();
        }

        return () => {
            if (interval) clearInterval(interval);
        };
    }, [isActive, seconds, onTimeUp]);

    const formatTime = (totalSeconds: number) => {
        const minutes = Math.floor(totalSeconds / 60);
        const secs = totalSeconds % 60;
        return `${minutes}:${secs.toString().padStart(2, '0')}`;
    };

    return (
        <div className="text-2xl font-mono font-bold text-red-500 bg-gray-800 px-4 py-2 rounded-lg shadow-lg">
            Time Remaining: {formatTime(seconds)}
        </div>
    );
};

export default Timer;
