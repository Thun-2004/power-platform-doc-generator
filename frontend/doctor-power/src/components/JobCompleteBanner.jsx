import React, { useEffect } from 'react';
import '../styles/App.css';

/**
 * Banner shown when job status is Completed or Failed.
 * Positioned top-center below the header. Green circle + "completed" or red circle + "failed".
 * Optional `message` shows backend error/details when status is Failed.
 */
const JobCompleteBanner = ({ status, message, onDismiss }) => {
  const isFailed = status === 'Failed';
  const accentWord = isFailed ? 'failed' : 'completed';
  const circleColor = isFailed
    ? 'bg-red-500 ring-2 ring-red-300'
    : 'bg-green-500 ring-2 ring-green-300';
  const accentTextColor = isFailed ? 'text-red-500' : 'text-green-500';
  const displayMessage = (message && String(message).trim()) || null;

  useEffect(() => {
    const t = setTimeout(() => {
      if (onDismiss) onDismiss();
    }, 5000);
    return () => clearTimeout(t);
  }, [onDismiss]);

  return (
    <div
      className="fixed left-1/2 -translate-x-1/2 top-20 z-50 flex items-center gap-3 px-10 py-5 bg-white border border-gray-200 rounded-lg shadow-md"
      role="status"
      aria-live="polite"
    >
      <div
        className={`shrink-0 w-4 h-4 rounded-full ${circleColor}`}
        aria-hidden
      />
      <div className="flex flex-col gap-0.5">
        <p className="text-sm font-bold text-gray-900 m-0">
          Document Generation has <span className={accentTextColor}>{accentWord}</span>
        </p>
        {isFailed && displayMessage && (
          <p className="text-sm text-gray-600 m-0 font-normal">{displayMessage}</p>
        )}
      </div>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          className="shrink-0 ml-1 p-1 text-gray-400 hover:text-gray-600 rounded"
          aria-label="Dismiss"
        >
          ×
        </button>
      )}
    </div>
  );
};

export default JobCompleteBanner;
