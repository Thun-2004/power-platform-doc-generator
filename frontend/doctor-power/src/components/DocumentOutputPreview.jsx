import React from 'react';
import { FileText, X } from 'lucide-react';
import '../styles/App.css';

const STAGES = ['Pending', 'Processing', 'Completed', 'Failed'];
const normalizeStatus = (s) =>
  (s && String(s).trim()) ? String(s).trim() : 'Pending';
const progressPercent = (status) => {
  const norm = normalizeStatus(status);
  const i = STAGES.findIndex((x) => x.toLowerCase() === norm.toLowerCase());
  if (i === -1) return 0;
  if (norm.toLowerCase() === 'completed' || norm.toLowerCase() === 'failed')
    return 100;
  return ((i + 1) / STAGES.length) * 100; // Pending 25%, Processing 50%
};

const DocumentOutputPreview = ({ outputItems, setPreviewFile, onDismiss, onRegenerate }) => {
  if (!outputItems?.length) return null;

  return (
    <div className="flex flex-col gap-4">
      {outputItems.map((item) => {
        const status = normalizeStatus(item.status);
        const percent = progressPercent(status);
        const isComplete =
          status.toLowerCase() === 'completed' && item.url;
        const isFailed = status.toLowerCase() === 'failed';
        const displayName = item.name ?? item.displayName;

        return (
          <div
            key={item.id}
            className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg transition-all hover:shadow-md"
          >
            <div
              className={`shrink-0 w-10 h-10 flex items-center justify-center rounded-lg ${
                isFailed ? 'bg-red-100' : 'bg-green-100'
              }`}
            >
              <FileText
                className={isFailed ? 'text-red-600' : 'text-green-700'}
                size={20}
              />
            </div>

            {isComplete ? (
              <>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {displayName}
                  </p>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <button
                    type="button"
                    onClick={() =>
                      setPreviewFile({
                        name: item.name,
                        url: item.url,
                        blob: item.blob,
                      })
                    }
                    className="text-gray-500 border border-gray-300 px-3 py-1 rounded-md text-sm cursor-pointer transition-all hover:brightness-110 focus:outline-none"
                  >
                    Preview
                  </button>
                  <a
                    href={item.url}
                    download={item.name}
                    className="text-gray-500 border border-gray-300 px-3 py-1.5 rounded-md text-sm cursor-pointer transition-all hover:brightness-110"
                  >
                    Download
                  </a>
                </div>
              </>
            ) : (
              <>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {displayName}
                  </p>
                  {isFailed && (
                    <p className="mt-1 text-xs font-medium text-red-600">
                      State: Failed
                    </p>
                  )}
                  {!isFailed && (
                    <div className="mt-2 h-2 bg-gray-200 rounded-full overflow-hidden">
                      <div
                        className="h-full rounded-full transition-all duration-300 bg-blue-500"
                        style={{ width: `${percent}%` }}
                      />
                    </div>
                  )}
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  {isFailed ? (
                    <>
                      {onRegenerate && (
                        <button
                          type="button"
                          onClick={() => onRegenerate(item.outputType)}
                          className="text-gray-500 border border-gray-300 px-3 py-1 rounded-md text-sm cursor-pointer transition-all hover:brightness-110"
                        >
                          Regenerate
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => onDismiss(item.id)}
                        className="p-1 text-gray-400 hover:text-gray-600 rounded"
                        aria-label="Dismiss"
                      >
                        <X size={18} />
                      </button>
                    </>
                  ) : (
                    <>
                      <span className="text-sm font-medium text-gray-700">
                        State: {status}
                      </span>
                      <button
                        type="button"
                        onClick={() => onDismiss(item.id)}
                        className="p-1 text-gray-400 hover:text-gray-600 rounded"
                        aria-label="Dismiss"
                      >
                        <X size={18} />
                      </button>
                    </>
                  )}
                </div>
              </>
            )}
          </div>
        );
      })}
    </div>
  );
};

export default DocumentOutputPreview;
