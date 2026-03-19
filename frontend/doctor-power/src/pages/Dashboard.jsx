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
  const [fileTypes, setFileTypes] = useState([]);

  // Comes from backend /api/config/shared (Frontend:customPromptCharacterLimit)
  const [promptCharLimit, setPromptCharLimit] = useState(null);

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

  // Shared config from backend (SharedConfig.json)
  const [backendUrl, setBackendUrl] = useState("");
  const [aiModels, setAiModels] = useState([]);
  // LLM health status per model: { [modelName]: { isHealthy, error } }
  const [llmStatus, setLlmStatus] = useState({});
  // Prevent spamming console logs on every polling tick
  const loggedOutputErrorsRef = useRef(new Set());


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

  // Load shared config (backendUrl, aiModels) and LLM health from backend
  useEffect(() => {
        const loadSharedConfigAndLlmStatus = async () => {
      try {
        // Shared config
        const res = await axiosPublic.get("/api/config/shared");
        const data = res.data ?? {};
        if (data.backendUrl) setBackendUrl(data.backendUrl);
        if (Array.isArray(data.aiModels)) setAiModels(data.aiModels);
            if (typeof data.customPromptCharacterLimit === "number")
              setPromptCharLimit(data.customPromptCharacterLimit);

            if (Array.isArray(data.generatedOutputTypes))
              setFileTypes(data.generatedOutputTypes);

        // LLM status (optional endpoint)
        try {
          const statusRes = await axiosPublic.get("/api/config/llm-status");
          const statusArray = statusRes.data ?? [];
          const statusMap = {};
          for (const s of statusArray) {
            // backend returns { model, isHealthy, error }
            if (s.model) {
              statusMap[s.model] = {
                isHealthy: !!s.isHealthy,
                error: s.error || null,
              };
            }
          }
          setLlmStatus(statusMap);
        } catch (statusErr) {
          console.warn("Failed to load LLM status", statusErr);
        }
      } catch (err) {
        console.error("Failed to load shared config", err);
      }
    };
    loadSharedConfigAndLlmStatus();
  }, []);

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
        // Pass through the selected model name (or 'none') to backend
        formData.append('LlmModel', selectedLLM);

        let response;
        try {
          response = await axiosPublic.post('/api/File/generate', formData);
        } catch (postErr) {
          const msg = getErrorMessage(postErr);
          console.log(`[SubmitError] ${msg}`, postErr?.response?.data ?? postErr);
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
              // Log backend error to browser console (once per output type)
              if (status === 'Failed' && err) {
                const key = `${jobId}:${item.outputType}`;
                if (!loggedOutputErrorsRef.current.has(key)) {
                  loggedOutputErrorsRef.current.add(key);
                  console.log(`[JobError] jobId=${jobId} outputType=${item.outputType}:`, err);
                }
              }
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
              const firstFailedLabel =
                (fileTypes &&
                  Array.isArray(fileTypes) &&
                  fileTypes.find((t) => t.id === firstType)?.title) ||
                firstType;
              setJobCompleteStatus('Failed');
              // Keep message short: only tell which output type failed.
              setJobCompleteMessage(`\nFailed to generate: ${firstFailedLabel}`);
            } else {
              // partial failure: some completed, some failed
              const firstFailedType = failedTypes[0];
              const firstFailedLabel =
                (fileTypes &&
                  Array.isArray(fileTypes) &&
                  fileTypes.find((t) => t.id === firstFailedType)?.title) ||
                firstFailedType;
              const base =
                `Generation partially completed. \nFailed: ${firstFailedLabel}. \nClick Regenerate to try again.`;
              const msg = base;
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
        formData.append('LlmModel', selectedLLM);

        let response;
        try {
          response = await axiosPublic.post('/api/File/generate', formData);
        } catch (postErr) {
          const msg = getErrorMessage(postErr);
          console.log(`[RegenerateSubmitError] ${msg}`, postErr?.response?.data ?? postErr);
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
          const errorsRaw = statusData.Errors ?? statusData.errors ?? {};
          const rawStatus = progressRaw[outputType];
          const status =
            (rawStatus && String(rawStatus).trim()) || 'Processing';

          setOutputItems((prev) =>
            prev.map((item) =>
              item.outputType === outputType
                ? {
                    ...item,
                    status,
                    error:
                      (errorsRaw &&
                        typeof errorsRaw === 'object' &&
                        errorsRaw[item.outputType]) ||
                      item.error,
                  }
                : item
            )
          );
          // If it failed, log error once (using the same ref to avoid spam)
          if (status === 'Failed') {
            const err =
              (errorsRaw &&
                typeof errorsRaw === 'object' &&
                errorsRaw[outputType]) ||
              null;
            if (err) {
              const key = `${jobId}:${outputType}`;
              if (!loggedOutputErrorsRef.current.has(key)) {
                loggedOutputErrorsRef.current.add(key);
                console.log(`[JobError] jobId=${jobId} outputType=${outputType}:`, err);
              }
            }
          }

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
      <main className="flex-1 p-4 sm:p-6 md:p-7 lg:p-8 xl:p-9 bg-white m-2 sm:m-3 md:m-4 lg:m-4 rounded-xl shadow-sm overflow-y-auto">
        {/* Upload File Section */}
        <section className="mb-6 sm:mb-7 md:mb-8 lg:mb-9">
          <h2 className="text-title">Upload File</h2>
          <div 
            className={`w-full min-h-[200px] border-2 border-dashed rounded-xl bg-gray-50 flex justify-center items-center p-4 sm:p-6 md:p-7 lg:p-8 transition-colors ${
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
                <div className="mt-4 p-2 sm:p-3 md:p-3 lg:p-4 border-1 border-gray-300 rounded-lg flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 md:gap-4">
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
        <section className="mb-6 sm:mb-7 md:mb-8 lg:mb-9">
          <div className="w-full">
            <h2 className="text-title">Select output file types</h2>

            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-2 gap-2 sm:gap-3 md:gap-4 lg:gap-5">
              {fileTypes.map((type) => (
                <DiagramSelectionBox
                  key={type.id}
                  type={type}
                  selectedModes={selectedModes}
                  toggleSelected={toggleSelected}
                  charLimit={promptCharLimit ?? 250}
                />
              ))}
            </div>

        </div>
        </section>

        <section className="mb-6 sm:mb-7 md:mb-8 lg:mb-9">
          <div className="w-full">
            <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3 md:gap-4 lg:gap-5">
              <h2 className="text-title m-0">Select LLM model</h2>
              <div className="flex flex-col gap-1 w-full sm:w-auto">
                <select
                  value={selectedLLM}
                  onChange={(e) => setSelectedLLM(e.target.value)}
                  className="w-full sm:w-40 md:w-48 lg:w-56 rounded-md border border-gray-300 bg-white px-3 py-2 text-sm md:text-base shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                >
                  <option value="">Select model</option>
                  <option value="none">No LLM</option>
                  {aiModels.map((m) => {
                    const status = llmStatus[m];
                    const healthy = status ? status.isHealthy !== false : true;
                    const label = healthy
                      ? m
                      : `${m} (unavailable – contact admin)`;
                    return (
                      <option
                        key={m}
                        value={m}
                        disabled={!healthy}
                        className={!healthy ? "opacity-50" : ""}
                      >
                        {label}
                      </option>
                    );
                  })}
                </select>
                {Object.values(llmStatus).some(
                  (s) => s && s.isHealthy === false
                ) && (
                  <p className="text-xs text-amber-700">
                    If some AI models are currently unavailable, contact
                    your administrator.
                  </p>
                )}
              </div>
              <div className="sm:ml-auto w-full sm:w-auto mt-3 sm:mt-0">
                <button
                  onClick={onGenerateOutputFile}
                  className="btn-theme text-title text-white"
                >
                  Generate
                </button>
              </div>
            </div>

        </div>
        </section>

        



        {/* Output Section */}
        <section className="mt-6 sm:mt-7 md:mt-8 lg:mt-9">
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 md:gap-4 lg:gap-6 mb-4 md:mb-5 lg:mb-6">
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
