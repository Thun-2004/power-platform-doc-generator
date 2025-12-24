import React, { useState, useRef } from "react";
import OutputSelect from '../fileOutput';

const Dashboard = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [activeMenu, setActiveMenu] = useState('home');

  const [isGenerating, setIsGenerating] = useState(false);
  const [generated, setGenerated] = useState(false);
  const [outputFiles, setOutputFiles] = useState([]); // start empty

  // NEW: upload list state
  const [uploads, setUploads] = useState([]);
  const xhrMapRef = useRef(new Map()); // id -> XMLHttpRequest

  const makeId = () => crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;

  const startUpload = (file) => {
    const id = makeId();

    // Add UI row immediately (shows spinner + ❌)
    setUploads((prev) => [
      { id, name: file.name, status: "uploading" }, // uploading | done | error
      ...prev,
    ]);

    // --- REAL upload (cancellable) ---
    const xhr = new XMLHttpRequest();
    xhrMapRef.current.set(id, xhr);

    xhr.open("POST", "/api/upload"); // <-- change to your backend route
    // xhr.responseType = "json";

    xhr.onload = () => {
      xhrMapRef.current.delete(id);
      if (xhr.status >= 200 && xhr.status < 300) {
        setUploads((prev) =>
          prev.map((u) => (u.id === id ? { ...u, status: "done" } : u))
        );
      } else {
        setUploads((prev) =>
          prev.map((u) => (u.id === id ? { ...u, status: "error" } : u))
        );
      }
    };

    xhr.onerror = () => {
      xhrMapRef.current.delete(id);
      setUploads((prev) =>
        prev.map((u) => (u.id === id ? { ...u, status: "error" } : u))
      );
    };

    xhr.onabort = () => {
      xhrMapRef.current.delete(id);
      // remove row completely when cancelled (like your screenshot)
      setUploads((prev) => prev.filter((u) => u.id !== id));
    };

    const fd = new FormData();
    fd.append("file", file);
    xhr.send(fd);
  };

  const cancelUpload = (id) => {
    const xhr = xhrMapRef.current.get(id);
    if (xhr) xhr.abort();
  };

  const deleteUploaded = (id) => {
    // (optional) call backend to delete file by id if you store it server-side
    setUploads((prev) => prev.filter((u) => u.id !== id));
  };
  
  // Sample output files data
  // const outputFiles = [
  //   { id: 1, name: "Client1-ER-diagram.pdf", type: "pdf" }
  // ];
  const handleGenerate = async (selectedModes) => {
  // Example: only allow generate if at least 1 upload is done
    const readyFiles = uploads.filter(u => u.status === "done");
    if (readyFiles.length === 0) return;

    setIsGenerating(true);
    setGenerated(false);
    setOutputFiles([]);

    try {
      // TODO: call backend generate endpoint here
      // e.g. POST /api/generate with fileId(s) + modes
      // For now, fake delay + fake output:
      await new Promise((r) => setTimeout(r, 1200));

      setOutputFiles([
        { id: 1, name: "Client1-ER-diagram.pdf", type: "pdf" },
        { id: 2, name: "Client2-ER-diagram.pdf", type: "pdf" },
      ]);
      setGenerated(true);
    } catch (e) {
      console.error(e);
      setGenerated(false);
    } finally {
      setIsGenerating(false);
    }
  };

  const SmallSpinner = () => (
    <svg className="w-4 h-4 animate-spin" viewBox="0 0 24 24" fill="none">
      <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" className="text-gray-300" />
      <path d="M22 12a10 10 0 0 1-10 10" stroke="currentColor" strokeWidth="3" strokeLinecap="round" className="text-blue-600"/>
    </svg>
  );

  //upload component
  const UploadRow = ({ item, onCancel, onDelete }) => {
    const isUploading = item.status === "uploading";
    const isDone = item.status === "done";

    return (
      <div className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg">
        <div className="w-10 h-10 flex items-center justify-center bg-green-50 rounded-lg">
          {/* file icon */}
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6Z" stroke="#16a34a" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            <path d="M14 2v6h6" stroke="#16a34a" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </div>

        <div className="flex-1">
          <p className="text-sm font-medium text-gray-800">{item.name}</p>

          {/* status line under filename */}
          <div className="mt-1 flex items-center gap-2">
            {isUploading ? (
              <>
                {/* spinner */}
                <svg className="w-4 h-4 animate-spin" viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" className="text-gray-300" />
                  <path d="M22 12a10 10 0 0 1-10 10" stroke="currentColor" strokeWidth="3" strokeLinecap="round" className="text-blue-600"/>
                </svg>
                <span className="text-xs text-gray-500">Uploading...</span>
              </>
            ) : isDone ? (
              <>
                {/* green tick */}
                <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none">
                  <path d="M20 6 9 17l-5-5" stroke="#22c55e" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"/>
                </svg>
                <span className="text-xs text-green-600">Completed</span>
              </>
            ) : (
              <span className="text-xs text-green-600">Completed</span>
            )}
          </div>
        </div>

        {/* Right side action: ❌ while uploading, 🗑 when done */}
        {isUploading ? (
          <button
            onClick={() => onCancel(item.id)}
            className="p-2 rounded-md hover:bg-gray-100"
            title="Cancel upload"
          >
            <svg className="w-5 h-5 text-gray-500" viewBox="0 0 24 24" fill="none">
              <path d="M18 6 6 18M6 6l12 12" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"/>
            </svg>
          </button>
        ) : (
          <button
            onClick={() => onDelete(item.id)}
            className="p-2 rounded-md hover:bg-gray-100"
            title="Delete"
          >
            <svg className="w-5 h-5 text-gray-500" viewBox="0 0 24 24" fill="none">
              <path d="M3 6h18" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"/>
              <path d="M8 6V4h8v2" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"/>
              <path d="M6 6l1 16h10l1-16" stroke="currentColor" strokeWidth="2.5" strokeLinejoin="round"/>
            </svg>
          </button>
        )}
      </div>
    );
  };

  return (
    <div className="flex flex-1 overflow-hidden">
      {/* Sidebar */}
      <aside className={`w-64 bg-gray-100 p-6 flex flex-col gap-3 flex-shrink-0 transition-all ${isSidebarOpen ? '' : ''}`}>
        <div className="flex items-center">
          <button 
            className="bg-transparent border-none cursor-pointer p-2 flex items-center justify-center"
            onClick={() => setIsSidebarOpen(!isSidebarOpen)}
          >
            <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <span className="text-xl font-semibold text-gray-800">Menu</span>
        </div>
        <nav className="flex flex-col gap-1">
          <div 
            className={`flex items-center gap-3 px-4 py-3 rounded-lg cursor-pointer transition-all ${
              activeMenu === 'home' 
                ? 'bg-blue-600 text-white' 
                : 'text-gray-600 hover:bg-gray-200'
            }`}
            onClick={() => setActiveMenu('home')}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
            </svg>
            <span className="text-sm font-medium">Home</span>
          </div>
          <div 
            className={`flex items-center gap-3 px-4 py-3 rounded-lg cursor-pointer transition-all ${
              activeMenu === 'collections' 
                ? 'bg-blue-600 text-white' 
                : 'text-gray-600 hover:bg-gray-200'
            }`}
            onClick={() => setActiveMenu('collections')}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
            </svg>
            <span className="text-sm font-medium">Your collections</span>
          </div>
          <div 
            className={`flex items-center gap-3 px-4 py-3 rounded-lg cursor-pointer transition-all ${
              activeMenu === 'profile' 
                ? 'bg-blue-600 text-white' 
                : 'text-gray-600 hover:bg-gray-200'
            }`}
            onClick={() => setActiveMenu('profile')}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            <span className="text-sm font-medium">Profile</span>
          </div>
        </nav>
      </aside>

      {/* Main Area -> dynamic change */}
      <main className="flex-1 p-8 bg-white m-4 rounded-xl shadow-sm overflow-y-auto">
        {/* Upload File Section */}
        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-gray-800">Upload File</h2>
          <div className="w-full min-h-[200px] border-2 border-dashed border-gray-300 rounded-xl bg-gray-50 flex justify-center items-center p-8">
            <div className="text-center flex flex-col items-center gap-4">
              <p className="text-base text-gray-600 m-0">Drag and drop your file here</p>
              <p className="text-sm text-gray-400 m-0">Max 200 MB, .msapp, .zip, .pdf</p>
              <label className="bg-blue-600 text-white px-6 py-2.5 rounded-lg cursor-pointer inline-block text-sm font-medium transition-all hover:brightness-110 mt-2">
                Browse File
                <input
                  type="file"
                  accept=".png,.jpeg,.jpg,.mp3,.mp4"
                  className="hidden"
                  // onChange={(e) => {
                  //   if (e.target.files && e.target.files[0]) {
                  //     const file = e.target.files[0];
                  //     console.log('Selected file:', file.name);
                  //   }
                  // }}
                  onChange={(e) => {
                    const f = e.target.files?.[0];
                    if (!f) return;
                    startUpload(f);
                    e.target.value = ""; // allows re-selecting same file again
                  }}
                />
              </label>
            </div>
          </div>
          {uploads.length > 0 && (
            <div className="mt-4 flex flex-col gap-3">
              {uploads.map((u) => (
                <UploadRow
                  key={u.id}
                  item={u}
                  onCancel={cancelUpload}
                  onDelete={deleteUploaded}
                />
              ))}
            </div>
          )}
        </section>

        {/*Output File Types Section */}
        <section className="mb-8">
          <OutputSelect 
            onGenerate={handleGenerate}
            isGenerating={isGenerating}
          />
        </section>

        {/* Output Section */}
        {/* <section className="mt-8">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-xl font-semibold text-gray-800">Output(2)</h2>
            <button className="bg-blue-600 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer transition-all hover:brightness-110">Download all(2)</button>
          </div>
          <div className="flex flex-col gap-4">
            {outputFiles.map((file) => (
              <div key={file.id} className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg transition-all hover:shadow-md cursor-pointer">
                <div className="flex items-center justify-center">
                  <div className="w-10 h-10 flex items-center justify-center bg-blue-50 rounded-lg">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M14 2H6C5.46957 2 4.96086 2.21071 4.58579 2.58579C4.21071 2.96086 4 3.46957 4 4V20C4 20.5304 4.21071 21.0391 4.58579 21.4142C4.96086 21.7893 5.46957 22 6 22H18C18.5304 22 19.0391 21.7893 19.4142 21.4142C19.7893 21.0391 20 20.5304 20 20V8L14 2Z" stroke="#3b82f6" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                      <path d="M14 2V8H20" stroke="#3b82f6" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                      <path d="M12 14V18" stroke="#3b82f6" strokeWidth="2" strokeLinecap="round"/>
                      <path d="M10 16H14" stroke="#3b82f6" strokeWidth="2" strokeLinecap="round"/>
                    </svg>
                  </div>
                </div>
                <span className="flex-1 text-sm text-gray-800 font-medium">{file.name}</span>
                <div className="flex gap-2">
                  <button className=" text-gray-500 border border-gray-300 px-3 py-1 rounded-md text-sm cursor-pointer transition-all hover:brightness-110">Preview</button>
                  <button className="text-gray-500 border border-gray-300 px-3 py-1.5 rounded-md text-sm cursor-pointer transition-all hover:brightness-110">Download</button>
                </div>
              </div>
            ))}
          </div>
        </section> */}
        <section className="mt-8">
          <div className="flex justify-between items-center mb-1">
            <h2 className="text-xl font-semibold text-gray-800">
              Output{generated ? `(${outputFiles.length})` : ""}
            </h2>

            {/* {isGenerating ? "Generating..." : "Generate documents"} */}

            {generated && (
              <button className="bg-blue-600 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer transition-all hover:brightness-110">
                Download all({outputFiles.length})
              </button>
            )}
          </div>

          {/* loading line under header */}
          {/* {isGenerating && (
            <div className="flex items-center gap-2 text-sm text-gray-500 mb-4">
              <SmallSpinner />
              <span>Generating...</span>
            </div>
          )} */}
          <SmallSpinner />

          {/* Only show outputs AFTER generate is done */}
          {generated && (
            <div className="flex flex-col gap-4 mt-4">
              {outputFiles.map((file) => (
                <div key={file.id} className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg transition-all hover:shadow-md cursor-pointer">
                  <div className="w-10 h-10 flex items-center justify-center bg-blue-50 rounded-lg">
                    {/* your icon */}
                    ...
                  </div>

                  <span className="flex-1 text-sm text-gray-800 font-medium">{file.name}</span>

                  <div className="flex gap-2">
                    <button className="text-gray-500 border border-gray-300 px-3 py-1 rounded-md text-sm cursor-pointer transition-all hover:brightness-110">
                      Preview
                    </button>
                    <button className="text-gray-500 border border-gray-300 px-3 py-1.5 rounded-md text-sm cursor-pointer transition-all hover:brightness-110">
                      Download
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </main>
    </div>
  );
};

export default Dashboard;
