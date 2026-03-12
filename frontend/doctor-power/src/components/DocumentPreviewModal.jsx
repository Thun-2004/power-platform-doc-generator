import React, { useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import { renderAsync } from 'docx-preview';
import { Document, Page, pdfjs } from 'react-pdf';
import * as XLSX from 'xlsx';

// Worker must match react-pdf's bundled pdfjs version (e.g. 4.8.69). Use CDN with that version.
pdfjs.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjs.version}/pdf.worker.min.mjs`;

const DocumentPreviewModal = ({ file, isOpen, onClose }) => {
  const containerRef = useRef(null);
  const [pdfUrl, setPdfUrl] = useState(null);
  const [pdfError, setPdfError] = useState(null);
  const [pdfNumPages, setPdfNumPages] = useState(null);
  const [pdfPageNumber, setPdfPageNumber] = useState(1);
  const [excelSheets, setExcelSheets] = useState([]);
  const [activeSheet, setActiveSheet] = useState(0);
  const [excelData, setExcelData] = useState([]);

  const getFileType = (fileName) => {
    if (!fileName) return null;
    const ext = fileName.toLowerCase().split('.').pop();
    if (ext === 'pdf') return 'pdf';
    if (['xlsx', 'xls', 'csv'].includes(ext)) return 'excel';
    if (ext === 'docx') return 'docx';
    return null;
  };

  const handlePdfLoadSuccess = ({ numPages }) => {
    setPdfNumPages(numPages);
    setPdfError(null);
  };

  const handlePdfLoadError = (err) => {
    console.error('PDF load error', err);
    setPdfError(err?.message || 'Failed to load PDF');
  };

  const loadExcelFile = async (blob) => {
    try {
      const arrayBuffer = await blob.arrayBuffer();
      const workbook = XLSX.read(arrayBuffer, { type: 'array' });
      setExcelSheets(workbook.SheetNames);
      
      const firstSheetName = workbook.SheetNames[0];
      const worksheet = workbook.Sheets[firstSheetName];
      const data = XLSX.utils.sheet_to_json(worksheet, { header: 1 });
      setExcelData(data);
      setActiveSheet(0);
    } catch (err) {
      console.error('Failed to load Excel file', err);
      if (containerRef.current) {
        containerRef.current.innerHTML = `<div class="p-4 text-red-600">Failed to load Excel file: ${err.message}</div>`;
      }
    }
  };

  const handleSheetChange = async (sheetIndex) => {
    try {
      const arrayBuffer = await file.blob.arrayBuffer();
      const workbook = XLSX.read(arrayBuffer, { type: 'array' });
      const sheetName = workbook.SheetNames[sheetIndex];
      const worksheet = workbook.Sheets[sheetName];
      const data = XLSX.utils.sheet_to_json(worksheet, { header: 1 });
      setExcelData(data);
      setActiveSheet(sheetIndex);
    } catch (err) {
      console.error('Failed to switch sheet', err);
    }
  };

  const renderDocx = async () => {
    try {
      if (containerRef.current) {
        containerRef.current.innerHTML = '';
      }
      await renderAsync(file.blob, containerRef.current);
    } catch (err) {
      console.error('Failed to render DOCX', err);
      if (containerRef.current) {
        containerRef.current.innerHTML = `<div class="p-4 text-red-600">Failed to render document: ${err.message}</div>`;
      }
    }
  };

  useEffect(() => {
    if (!isOpen || !file) return;

    const fileType = getFileType(file.name);

    if (fileType === 'docx') {
      renderDocx();
    } else if (fileType === 'excel') {
      loadExcelFile(file.blob);
    } else if (fileType === 'pdf') {
      setPdfError(null);
      setPdfPageNumber(1);
      setPdfNumPages(null);
      const blob =
        file.blob instanceof Blob && file.blob.type === 'application/pdf'
          ? file.blob
          : new Blob([file.blob], { type: 'application/pdf' });
      const url = URL.createObjectURL(blob);
      setPdfUrl(url);
      return () => {
        URL.revokeObjectURL(url);
        setPdfUrl(null);
      };
    } else {
      setPdfUrl(null);
      setPdfError(null);
    }
  }, [isOpen, file]);

  if (!isOpen) return null;

  const fileType = getFileType(file?.name);

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4 outline-none">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col outline-none">
        {/* Header for doc */}
        <div className="flex justify-between items-center p-4 border-b border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800">{file?.name}</h3>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700 transition-colors focus:outline-none"
          >
            <X size={24} />
          </button>
        </div>

        {/* Doc Container */}
        <div className="flex-1 overflow-auto p-4 bg-gray-50">
          {fileType === 'docx' && (
            <div
              ref={containerRef}
              className="docx-preview-container bg-white p-8 mx-auto max-w-3xl border-0 outline-none"
            />
          )}

          {fileType === 'pdf' && (
            <div className="flex flex-col items-center gap-4 border-0 outline-none">
              {pdfError && (
                <div className="p-4 bg-red-50 text-red-700 rounded-lg w-full max-w-md">
                  {pdfError}
                </div>
              )}
              {pdfUrl && !pdfError && (
                <Document
                  file={{ url: pdfUrl }}
                  onLoadSuccess={handlePdfLoadSuccess}
                  onLoadError={handlePdfLoadError}
                  loading={
                    <div className="p-4 text-gray-600">Loading PDF…</div>
                  }
                  className="flex flex-col items-center border-0 outline-none"
                >
                  <Page
                    pageNumber={pdfPageNumber}
                    renderTextLayer={false}
                    renderAnnotationLayer={false}
                    className="max-w-full border-0 outline-none [&_canvas]:outline-none"
                    scale={1.5}
                  />
                </Document>
              )}
              {pdfNumPages && (
                <div className="flex items-center gap-4 p-4 bg-white rounded-lg shadow-sm">
                  <button
                    onClick={() => setPdfPageNumber(Math.max(1, pdfPageNumber - 1))}
                    disabled={pdfPageNumber === 1}
                    className="px-4 py-2 bg-blue-600 text-white rounded-md disabled:bg-gray-300 hover:bg-blue-700"
                  >
                    Previous
                  </button>
                  <span className="text-gray-600 font-medium">
                    Page {pdfPageNumber} of {pdfNumPages}
                  </span>
                  <button
                    onClick={() => setPdfPageNumber(Math.min(pdfNumPages, pdfPageNumber + 1))}
                    disabled={pdfPageNumber === pdfNumPages}
                    className="px-4 py-2 bg-blue-600 text-white rounded-md disabled:bg-gray-300 hover:bg-blue-700"
                  >
                    Next
                  </button>
                </div>
              )}
            </div>
          )}

          {fileType === 'excel' && (
            <div className="flex flex-col gap-4">
              {excelSheets.length > 1 && (
                <div className="flex gap-2 overflow-x-auto pb-2">
                  {excelSheets.map((sheetName, idx) => (
                    <button
                      key={idx}
                      onClick={() => handleSheetChange(idx)}
                      className={`px-4 py-2 rounded-md whitespace-nowrap font-medium transition-colors ${
                        activeSheet === idx
                          ? 'bg-blue-600 text-white'
                          : 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-100'
                      }`}
                    >
                      {sheetName}
                    </button>
                  ))}
                </div>
              )}
              <div className="bg-white rounded-lg shadow-sm overflow-x-auto">
                <table className="w-full border-collapse">
                  <tbody>
                    {excelData.map((row, rowIdx) => (
                      <tr key={rowIdx} className="border-b border-gray-200 hover:bg-gray-50">
                        {row.map((cell, cellIdx) => (
                          <td
                            key={cellIdx}
                            className="px-4 py-2 border-r border-gray-200 text-sm text-gray-700"
                          >
                            {cell}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {!fileType && (
            <div className="p-4 text-center text-gray-600">
              Unsupported file type. Please download the file to view it.
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DocumentPreviewModal;
