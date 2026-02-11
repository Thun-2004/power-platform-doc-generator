import React, { useState, useRef, useEffect } from "react";
import { FileCheck,Trash2 } from 'lucide-react';
import axiosPublic from "../api/axios";
import uploadFile from "../api/file";
import DocumentPreviewModal from "../components/DocumentPreviewModal";
import DiagramSelectionBox from "../components/DiagramSelectionBox";

//temp
import axios from 'axios';


const Dashboard = () => {

  const fileTypes = [
    { id: "er", title: "ER diagram", desc: "Dummy text for generating an ER diagram"},
    { id: "ui", title: "UI-Hierarchy flow", desc: "Dummy text for generating a UI-hierarchy"},
    { id: "program", title: "Program flow", desc: "Dummy text for generating a program-flow"},
    { id: "ai", title: "Dummy AI", desc: "Dummy option for assigning some AI task"},
  ];
  
  const [outputFiles, setOutputFiles] = useState([]);
  const [previewFile, setPreviewFile] = useState(null);

  //FIXME: finish axios private for this
  // const axiosPrivate = useAxiosPrivate();

  const [selectedFile, setSelectedFile] = useState(null);
  const [selectedModes, setSelectedModes] = useState([]);
  const [progress, setProgress] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const [isDragOver, setIsDragOver] = useState(false);


  const abortRef = useRef(null);//like useState but not rerender 
  const fileInputRef = useRef(null); // to clear the DOM input val

  const isZipFile = (file) => {
    return file && file.name.toLowerCase().endsWith('.zip');
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
            setSelectedFile(f);
            setProgress(0);
            try {
                if (fileInputRef.current) fileInputRef.current.value = "";
            } catch (e) {
                
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

    const onGenerateOutputFile = async () => {
      // Call backend to get generated document and present in Output
      try {
        setIsUploading(true);

        //Get prompts from selected output types so that they can be passed to the backend
        let prompts = {}
        selectedModes.forEach((m) => prompts[m] = document.getElementById(m).value)
        console.log(prompts)

        const response = await axiosPublic.get('/api/File/getDocument', {
          responseType: 'blob'
        });

        console.log(response.data);

        const contentDisposition = response.headers['content-disposition'] || '';
        let fileName = 'generated_document';
        const fileNameMatch = /filename=(?:"?)([^;\"]+)/i.exec(contentDisposition);
        if (fileNameMatch && fileNameMatch[1]) fileName = fileNameMatch[1].replace(/\"/g, '');

        const blob = new Blob([response.data], { type: response.data.type || 'application/octet-stream' });
        const url = URL.createObjectURL(blob);

        setOutputFiles(prev => [
          ...prev,
          {
            id: Date.now(),
            name: fileName,
            url,
            blob
          }
        ]);

      } catch (err) {
        console.error('Failed to fetch document', err);
      } finally {
        setIsUploading(false);
      }
    };

    const test_call = onGenerateOutputFile;

  const onCancelUpload = () => {
      abortRef.current?.abort(); 
  }

  return (
      <>
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

            <div className="mt-6">
                <button onClick={test_call} className="btn-theme">Generate</button>
            </div>
        </div>
        </section>

        {/* Output Section */}
        <section className="mt-8">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-title">Output(2)</h2>
            <button className="btn-theme">Download all(2)</button>
          </div>
          <div className="flex flex-col gap-4">
            {outputFiles.map((file) => (
              <div key={file.id} className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg transition-all hover:shadow-md">
                <div className="flex items-center justify-center">
                  <div className="w-10 h-10 flex items-center justify-center bg-blue-50 rounded-lg">
                    <FileCheck color="#3b82f6" size={20} />
                  </div>
                </div>
                <span className="flex-1 text-sm text-gray-800 font-medium">{file.name}</span>
                <div className="flex gap-2">
                  <button onClick={() => setPreviewFile(file)} className=" text-gray-500 border border-gray-300 px-3 py-1 rounded-md text-sm cursor-pointer transition-all hover:brightness-110">Preview</button>
                  <a href={file.url} download={file.name} className="text-gray-500 border border-gray-300 px-3 py-1.5 rounded-md text-sm cursor-pointer transition-all hover:brightness-110">Download</a>
                </div>
              </div>
            ))}
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
