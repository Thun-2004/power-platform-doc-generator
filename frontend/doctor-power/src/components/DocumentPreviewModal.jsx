import React, { useEffect, useRef } from 'react';
import { X } from 'lucide-react';
import { renderAsync } from 'docx-preview';

const DocumentPreviewModal = ({ file, isOpen, onClose }) => {
  const containerRef = useRef(null);

  useEffect(() => {
    if (!isOpen || !file || !containerRef.current) return;

    const renderDocument = async () => {
      try {

        if (containerRef.current) {
          containerRef.current.innerHTML = '';
        }

        await renderAsync(file.blob, containerRef.current);
      } catch (err) {
        console.error('Failed to render document', err);
        if (containerRef.current) {
          containerRef.current.innerHTML = `<div class="p-4 text-red-600">Failed to render document: ${err.message}</div>`;
        }
      }
    };

    renderDocument();
  }, [isOpen, file]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col">
        {/* Header for doc */}
        <div className="flex justify-between items-center p-4 border-b border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800">{file?.name}</h3>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700 transition-colors"
          >
            <X size={24} />
          </button>
        </div>

        {/* Doc Container */}
        <div className="flex-1 overflow-auto p-4 bg-gray-50">
          <div
            ref={containerRef}
            className="bg-white p-8 mx-auto max-w-3xl shadow-sm"
          />
        </div>
      </div>
    </div>
  );
};

export default DocumentPreviewModal;
