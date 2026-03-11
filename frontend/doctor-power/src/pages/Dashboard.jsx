import React, { useState, useRef, useEffect } from "react";
import { FileCheck,Trash2 } from 'lucide-react';
import axiosPublic from "../api/axios";
import uploadFile from "../api/file";
import DocumentPreviewModal from "../components/DocumentPreviewModal";
import DiagramSelectionBox from "../components/DiagramSelectionBox";
import DocumentOutputPreview from "../components/DocumentOutputPreview";
import JobCompleteBanner from "../components/JobCompleteBanner";

//temp
import axios from 'axios';

const Dashboard = () => {
  const fileTypes = [
    { id: "overview", title: "Overview", desc: "A high-level summary of the solution, including key components such as canvas apps, workflows, screens, and environment variables."},
    { id: "workflows", title: "Workflows", desc: "Detailed descriptions of the Power Automate workflows in the solution, including triggers, actions, connectors used, and their purpose."},
    { id: "faq", title: "Frequently Asked Questions", desc: "A concise list of frequently asked questions about the solution, explaining common functionality and how different components interact"},
    { id: "diagrams", title: "Diagrams", desc: "Visual architecture diagrams showing the relationships between canvas apps, workflows, and environment variables."},
    { id: "erd", title: "ER diagram", desc: "A structured diagram illustrating relationships between apps, screens, workflows, connectors, and environment variables within the solution."},
    { id: "environment-variables", title: "Environment variables", desc: "A structured table of environment variables used in the solution, including their type, description, and values across development, test, and production environments."}
  ];

  const statusSymbols = {
    'Pending': "⭘ Uploading Files",
    'Processing': "🔘 File is being processed by LLM",
    'Completed': "✅ Output should be produced",
    'Failed': "❌ request failed",
  }
  
  // Each item: { id, outputType, displayName, status, jobId, downloadUrl, name?, url?, blob? }
  const [outputItems, setOutputItems] = useState([]);
  const [previewFile, setPreviewFile] = useState(null);
  // Banner when whole job finishes: 'Completed' | 'Failed' | null
  const [jobCompleteStatus, setJobCompleteStatus] = useState(null);
  // Optional message for Failed (e.g. backend error text)
  const [jobCompleteMessage, setJobCompleteMessage] = useState(null);

  //FIXME: finish axios private for this
  // const axiosPrivate = useAxiosPrivate();

  const [selectedFile, setSelectedFile] = useState(null);
  const [selectedModes, setSelectedModes] = useState([]);
  // LLM model: '' = not selected yet, 'gpt-4.1' or 'No LLM'
  const [selectedLLM, setSelectedLLM] = useState('');
  const [progress, setProgress] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const [isDragOver, setIsDragOver] = useState(false);


  const abortRef = useRef(null);//like useState but not rerender 
  const fileInputRef = useRef(null); // to clear the DOM input val
 
  const delay = ms => new Promise(res => setTimeout(res, ms)); //call await delay(n) to wait n ms
  const tickLength = 100; //Tick length for calling API to check job status


  const isZipFile = (file) => {
    return file && file.name.toLowerCase().endsWith('.zip');
  };

  const resetToDefault = () => {
    setSelectedModes([]);
    setSelectedLLM('');
    setOutputItems((prev) => {
      prev.forEach((item) => {
        if (item.url) URL.revokeObjectURL(item.url);
      });
      return [];
    });
    setJobCompleteStatus(null);
    setJobCompleteMessage(null);
    setPreviewFile(null);
    fileTypes.forEach((type) => {
      const el = document.getElementById(type.id);
      if (el && el.tagName === 'TEXTAREA') el.value = '';
    });
    const jobStatusEl = document.getElementById('jobStatus');
    if (jobStatusEl) jobStatusEl.textContent = '';
  };

  const toggleSelected = (id) => {
    setSelectedModes(
      prev => 
        prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    ); 
  };


  useEffect(() => {
      console.log("Selected modes changed:", selectedModes);
    }, [selectedModes]);

  const onPickFile = (e) => {
      const f = e.target.files?.[0] ?? null;
      if (f && isZipFile(f)) {
        resetToDefault();
        setSelectedFile(f);
        setProgress(0);
      } else if (f) {
        alert('Please select a .zip file only.');
        // Clear the input
        if (fileInputRef.current) fileInputRef.current.value = "";
      }
  };

  const onDragOver = (e) => {
      e.preventDefault();
      setIsDragOver(true);
  };

  const onDragLeave = (e) => {
      e.preventDefault();
      setIsDragOver(false);
  };

  const onDrop = (e) => {
      e.preventDefault();
      setIsDragOver(false);
      const files = e.dataTransfer.files;
      if (files.length > 0) {
          const f = files[0];
          if (isZipFile(f)) {
            resetToDefault();
            setSelectedFile(f);
            setProgress(0);
            try {
                if (fileInputRef.current) fileInputRef.current.value = "";
            } catch (err) {
                console.error('Failed to clear file input', err);
            }
          } else {
            alert('Please select a .zip file only.');
          }
      }
  };

  //FIXME: might need to change in case multiple files are allowed
  const onRemoveFile = () => {

      setSelectedFile(null); 
      setProgress(0); 
      // clear the native input so selecting the same file again will fire change
      try {
        if (fileInputRef.current) fileInputRef.current.value = "";
      } catch (e) {
        // ignore
      }
  };

    const getErrorMessage = (err) => {
      const d = err?.response?.data;
      if (!d) return err?.message || 'Something went wrong';
      if (d.title === 'One or more validation errors occurred.') {
        return 'Invalid or corrupted zip file. Please upload a valid Power Platform solution package (.zip).';
      }
      return d.detail ?? d.Detail ?? d.message ?? d.Message ?? d.title ?? err?.message ?? 'Something went wrong';
    };

    const onGenerateOutputFile = async () => {
      try {
        setJobCompleteStatus(null);
        setJobCompleteMessage(null);

        const hasFile = !!selectedFile;
        const hasOptions = selectedModes.length > 0;
        const hasLLM = !!selectedLLM;
        if (!hasFile && !hasOptions) {
          setJobCompleteStatus('Failed');
          setJobCompleteMessage('Please select a file and at least one output type.');
          return;
        }
        if (!hasFile) {
          setJobCompleteStatus('Failed');
          setJobCompleteMessage('Please select a file.');
          return;
        }
        if (!hasOptions) {
          setJobCompleteStatus('Failed');
          setJobCompleteMessage('Please select at least one output type.');
          return;
        }
        if (!hasLLM) {
          setJobCompleteStatus('Failed');
          setJobCompleteMessage('Please select an LLM model.');
          return;
        }

        const selectedModesClone = [...selectedModes];
        setIsUploading(true);

        const formData = new FormData();
        formData.append('File', selectedFile);
        selectedModes.forEach((t) => {
          const prompt = document.getElementById(t)?.value?.trim() ?? '';
          formData.append('SelectedOutputTypes', prompt ? `${t}: ${prompt}` : t);
        });
        formData.append('LlmModel', selectedLLM === 'gpt-4.1' ? 'true' : 'false');

        let response;
        try {
          response = await axiosPublic.post('/api/File/generate', formData);
        } catch (postErr) {
          const msg = getErrorMessage(postErr);
          setJobCompleteStatus('Failed');
          setJobCompleteMessage(msg);
          return;
        }

        const data = response.data?.data ?? response.data;
        const jobId = data.JobId ?? data.jobId;
        const statusUrl = data.JobStatusUrl ?? data.jobStatusUrl;
        const outputApiUrls = data.OutputFilesMetas ?? data.outputFilesMetas ?? {};

        // Show one box per output type from the start
        const initialItems = selectedModesClone.map((outputType) => {
          const typeInfo = fileTypes.find((t) => t.id === outputType);
          return {
            id: `${jobId}-${outputType}-${Date.now()}`,
            outputType,
            displayName: typeInfo?.title ?? outputType,
            status: 'Pending',
            jobId,
            downloadUrl: outputApiUrls[outputType],
          };
        });
        setOutputItems(initialItems);

        // Poll job status and fetch files when Completed
        let done = false;
        const fetched = new Set(); // outputTypes we've already fetched
        while (!done) {
          await delay(tickLength);
          const statusRes = await axiosPublic.get(statusUrl);
          const statusData = statusRes.data?.data ?? statusRes.data ?? {};
          const jobStatus = statusData.JobStatus ?? statusData.jobStatus ?? '';
          const progressRaw = statusData.Progress ?? statusData.progress ?? {};
          const errorsRaw = statusData.Errors ?? statusData.errors ?? {};
          const progress = typeof progressRaw === 'object' && progressRaw !== null ? progressRaw : {};
          if (document.getElementById('jobStatus')) updateStatusSymbol(jobStatus);

          setOutputItems((prev) =>
            prev.map((item) => {
              const raw = progress[item.outputType];
              const err =
                (errorsRaw &&
                  typeof errorsRaw === 'object' &&
                  errorsRaw[item.outputType]) ||
                item.error;
              const status = (raw && String(raw).trim()) || item.status;
              return { ...item, status, error: err };
            })
          );

          // Fetch file for any output type that is now Completed and not yet fetched
          for (const outputType of selectedModesClone) {
            const statusForType = (progress[outputType] && String(progress[outputType]).trim()) || '';
            if (statusForType !== 'Completed' || fetched.has(outputType)) continue;
            fetched.add(outputType);
            const downloadUrl = outputApiUrls[outputType];
            if (!downloadUrl) continue;
            try {
              const fileRes = await axiosPublic.get(downloadUrl, {
                responseType: 'blob',
              });
              const contentDisposition =
                fileRes.headers['content-disposition'] || '';
              let fileName = 'generated_document';
              const fileNameMatch = /filename=(?:"?)([^;\"]+)/i.exec(
                contentDisposition
              );
              if (fileNameMatch?.[1]) fileName = fileNameMatch[1].replace(/"/g, "");
              const blob = new Blob([fileRes.data], {
                type: fileRes.data.type || 'application/octet-stream',
              });
              if (!fileName.includes('.')) {
                const mimeToExt = {
                  'application/vnd.openxmlformats-officedocument.wordprocessingml.document':
                    '.docx',
                  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet':
                    '.xlsx',
                  'application/pdf': '.pdf',
                  'application/zip': '.zip',
                };
                if (mimeToExt[blob.type]) fileName += mimeToExt[blob.type];
              }
              const url = URL.createObjectURL(blob);
              setOutputItems((prev) =>
                prev.map((item) =>
                  item.outputType === outputType
                    ? { ...item, name: fileName, url, blob, status: 'Completed' }
                    : item
                )
              );
            } catch (e) {
              console.error('Failed to fetch output file', outputType, e);
            }
          }

          const normalizedJobStatus = (jobStatus && String(jobStatus).trim()) || '';
          const allOutputsDone = selectedModesClone.every((t) => {
            const s = (progress[t] && String(progress[t]).trim()) || '';
            return s === 'Completed' || s === 'Failed';
          });

          done = normalizedJobStatus === 'Completed' || allOutputsDone;

          if (done) {
            const failedTypes = selectedModesClone.filter((t) => {
              const s = (progress[t] && String(progress[t]).trim()) || '';
              return s === 'Failed';
            });
            const anyFailed = failedTypes.length > 0;

            if (!anyFailed) {
              setJobCompleteStatus('Completed');
              setJobCompleteMessage(null);
            } else if (failedTypes.length === selectedModesClone.length) {
              // all requested outputs failed
              const firstType = failedTypes[0];
              const firstError =
                (errorsRaw &&
                  typeof errorsRaw === 'object' &&
                  errorsRaw[firstType]) ||
                null;
              setJobCompleteStatus('Failed');
              setJobCompleteMessage(firstError);
            } else {
              // partial failure: some completed, some failed
              const label = failedTypes.join(', ');
              const firstType = failedTypes[0];
              const firstError =
                (errorsRaw &&
                  typeof errorsRaw === 'object' &&
                  errorsRaw[firstType]) ||
                null;
              const base =
                `Generation partially completed but ${label} failed. Click Regenerate to try again.`;
              const msg = firstError ? `${base} (${firstError})` : base;
              setJobCompleteStatus('PartialFailed');
              setJobCompleteMessage(msg);
            }
          }
        }
      } catch (err) {
        console.error('Failed to generate document', err);
        const msg = getErrorMessage(err);
        setJobCompleteStatus('Failed');
        setJobCompleteMessage(msg);
      } finally {
        setIsUploading(false);
      }
    };

    const onDismissOutputItem = (id) => {
      setOutputItems((prev) => {
        const item = prev.find((i) => i.id === id);
        if (item?.url) URL.revokeObjectURL(item.url);
        return prev.filter((i) => i.id !== id);
      });
    };

    const onDownloadAll = () => {
      const completed = outputItems.filter((item) => item.url && item.name);
      if (completed.length === 0) return;
      completed.forEach((item, index) => {
        setTimeout(() => {
          const a = document.createElement('a');
          a.href = item.url;
          a.download = item.name ?? `document-${item.outputType}`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
        }, index * 200);
      });
    };

    const onRegenerateOutput = async (outputType) => {
      try {
        setJobCompleteStatus(null);
        setJobCompleteMessage(null);

        if (!selectedFile) {
          setJobCompleteStatus('Failed');
          setJobCompleteMessage('Please select a file before regenerating.');
          return;
        }
        if (!selectedLLM) {
          setJobCompleteStatus('Failed');
          setJobCompleteMessage('Please select an LLM model before regenerating.');
          return;
        }

        const prompt = document.getElementById(outputType)?.value?.trim() ?? '';
        const formData = new FormData();
        formData.append('File', selectedFile);
        formData.append(
          'SelectedOutputTypes',
          prompt ? `${outputType}: ${prompt}` : outputType
        );
        formData.append('LlmModel', selectedLLM === 'gpt-4.1' ? 'true' : 'false');

        let response;
        try {
          response = await axiosPublic.post('/api/File/generate', formData);
        } catch (postErr) {
          const msg = getErrorMessage(postErr);
          setJobCompleteStatus('Failed');
          setJobCompleteMessage(msg);
          return;
        }

        const data = response.data?.data ?? response.data;
        const jobId = data.JobId ?? data.jobId;
        const statusUrl = data.JobStatusUrl ?? data.jobStatusUrl;
        const outputApiUrls = data.OutputFilesMetas ?? data.outputFilesMetas ?? {};
        const downloadUrl = outputApiUrls[outputType];

        // mark this output as regenerating
        setOutputItems((prev) =>
          prev.map((item) =>
            item.outputType === outputType
              ? {
                  ...item,
                  status: 'Pending',
                  jobId,
                  downloadUrl,
                  // keep existing name/url until new one is ready
                }
              : item
          )
        );

        let done = false;
        while (!done) {
          await delay(tickLength);
          const statusRes = await axiosPublic.get(statusUrl);
          const statusData = statusRes.data?.data ?? statusRes.data ?? {};
          const progressRaw = statusData.Progress ?? statusData.progress ?? {};
          const rawStatus = progressRaw[outputType];
          const status =
            (rawStatus && String(rawStatus).trim()) || 'Processing';

          setOutputItems((prev) =>
            prev.map((item) =>
              item.outputType === outputType ? { ...item, status } : item
            )
          );

          if (status === 'Completed' || status === 'Failed') {
            done = true;

            if (status === 'Completed' && downloadUrl) {
              try {
                const fileRes = await axiosPublic.get(downloadUrl, {
                  responseType: 'blob',
                });
                const contentDisposition =
                  fileRes.headers['content-disposition'] || '';
                let fileName = 'generated_document';
                const fileNameMatch = /filename=(?:"?)([^;\"]+)/i.exec(
                  contentDisposition
                );
                if (fileNameMatch?.[1])
                  fileName = fileNameMatch[1].replace(/"/g, '');
                const blob = new Blob([fileRes.data], {
                  type: fileRes.data.type || 'application/octet-stream',
                });
                if (!fileName.includes('.')) {
                  const mimeToExt = {
                    'application/vnd.openxmlformats-officedocument.wordprocessingml.document':
                      '.docx',
                    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet':
                      '.xlsx',
                    'application/pdf': '.pdf',
                    'application/zip': '.zip',
                  };
                  if (mimeToExt[blob.type]) fileName += mimeToExt[blob.type];
                }
                const url = URL.createObjectURL(blob);
                setOutputItems((prev) =>
                  prev.map((item) =>
                    item.outputType === outputType
                      ? { ...item, name: fileName, url, blob, status: 'Completed' }
                      : item
                  )
                );
              } catch (e) {
                console.error('Failed to fetch regenerated output file', outputType, e);
              }
            }
          }
        }
      } catch (err) {
        console.error('Failed to regenerate output', err);
        const msg = getErrorMessage(err);
        setJobCompleteStatus('Failed');
        setJobCompleteMessage(msg);
      }
    };


  const onCancelUpload = () => {
      abortRef.current?.abort(); 
  };

  const updateStatusSymbol = (status) => {
    document.getElementById("jobStatus").innerText = statusSymbols[status];
  } ;

  return (
      <>
      {jobCompleteStatus && (
        <JobCompleteBanner
          status={jobCompleteStatus}
          message={jobCompleteMessage}
          onDismiss={() => {
            setJobCompleteStatus(null);
            setJobCompleteMessage(null);
          }}
        />
      )}
      <main className="flex-1 p-8 bg-white m-4 rounded-xl shadow-sm overflow-y-auto">
        {/* Upload File Section */}
        <section className="mb-8">
          <h2 className="text-title">Upload File</h2>
          <div 
            className={`w-full min-h-[200px] border-2 border-dashed rounded-xl bg-gray-50 flex justify-center items-center p-8 transition-colors ${
              isDragOver ? 'border-blue-400 bg-blue-50' : 'border-gray-300'
            }`}
            onDragOver={onDragOver}
            onDragLeave={onDragLeave}
            onDrop={onDrop}
          >
            <div className="text-center flex flex-col items-center gap-4">
              <p className="text-base text-gray-600 m-0">Drag and drop your file here</p>
              <p className="text-sm text-gray-400 m-0">Max 120 MB, only .ZIP accepted</p>
              <label className="btn-theme">
                Browse File
                <input
                  type="file"
                  accept=".zip"
                  className="hidden"
                  ref={fileInputRef} //set DOM input
                  onChange={(e) => {
                    onPickFile(e);
                    
                  }}
                />
              </label>
            </div>
          </div>
          
          {
              selectedFile && (
                <div className="mt-4 p-3 border-1 border-gray-300 rounded-lg flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="p-2 bg-amber-600 rounded-md">
                      <FileCheck color="#ffffff" size={20} />
                    </div>
                    <div className="flex flex-col gap-y-0">
                      <span className="text-md text-gray-800 font-medium">{selectedFile?.name}</span>
                      <span className="text-sm text-gray-800 font-medium">status: <span className="text-green-600 ml-1">✓</span></span>
                    </div>
                  </div>
                  
                  <button
                    onClick={onRemoveFile}
                    className="text-gray-500 hover:text-red-500"
                  >
                    <Trash2 />
                  </button>
                </div>
            )
          }
          
        </section>

        {/* Select Output File Types Section */}
        <section className="mb-8">
          <div className="w-full">
            <h2 className="text-title">Select output file types</h2>

            <div className="grid grid-cols-2 gap-4">
              {fileTypes.map((type) => <DiagramSelectionBox type={type} selectedModes={selectedModes} toggleSelected={toggleSelected}/>)}
            </div>

        </div>
        </section>

        <section className="mb-8">
          <div className="w-full">
            <div className="flex items-center max-w-md">
              <h2 className="text-title m-0">Select LLM model</h2>
              <select
                value={selectedLLM}
                onChange={(e) => setSelectedLLM(e.target.value)}
                className="ml-4 w-40 rounded-md border border-gray-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              >
                <option value="">Select model</option>
                <option value="gpt-4.1">gpt-4.1</option>
                <option value="none">No LLM</option>
              </select>
            </div>

            <div className="mt-6">
                <button onClick={onGenerateOutputFile} className="btn-theme text-title text-white">Generate</button>
                {/* <h2 id="jobStatus"></h2> */}
            </div>
        </div>
        </section>

        



        {/* Output Section */}
        <section className="mt-8">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-title">Output</h2>
            <button
              type="button"
              onClick={onDownloadAll}
              disabled={!outputItems.some((item) => item.url)}
              className="btn-theme disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Download all
            </button>
          </div>
          <div id="file-display" className="flex flex-col gap-4">
              <DocumentOutputPreview
                outputItems={outputItems}
                setPreviewFile={setPreviewFile}
                onDismiss={onDismissOutputItem}
                onRegenerate={onRegenerateOutput}
              />
          </div>
        </section>
      </main>
      <DocumentPreviewModal
        file={previewFile}
        isOpen={!!previewFile}
        onClose={() => setPreviewFile(null)}
      />
      </>
  );
};

export default Dashboard;
