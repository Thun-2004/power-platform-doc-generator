import React, { useState, useRef, useEffect } from "react";
import { FileCheck,Trash2 } from 'lucide-react';
import axiosPublic from "../api/axios";
import uploadFile from "../api/file";

//temp
import axios from 'axios';


const Dashboard = () => {

  const fileTypes = [
    { id: "er", title: "ER diagram", desc: "Dummy text for generating an ER diagram" },
    { id: "ui", title: "UI-Hierarchy flow", desc: "Dummy text for generating a UI-hierarchy" },
    { id: "program", title: "Program flow", desc: "Dummy text for generating a program-flow" },
    { id: "ai", title: "Dummy AI", desc: "Dummy option for assigning some AI task" },
  ];
  
  const outputFiles = [
    { id: 1, name: "Client1-ER-diagram.pdf", type: "pdf" }
  ];

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

      // console.log("Selected modes:", selectedModes);
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

  const onGenerateOutputFile = () => {
      if(!selectedFile) return;

      const fd = new FormData(); 
      fd.append("file", selectedFile); 
      selectedModes.forEach((m) => fd.append("modes", m)); 

      const controller = new AbortController();
      abortRef.current = controller; 

      try {
          setIsUploading(true);

          //FIXME: use axiosPrivate and complete uploadFile function
          const res = uploadFile()
      } catch (error) {
          if (error.name == "CanceledError"){
              console.log("Upload canceled");
          } else {
              console.error(error);
          }
      } finally {
          setIsUploading(false);
      }
  };

  const test_call = async () => {
      const fd = new FormData(); 

      fd.append("File", selectedFile);
      fd.append("OutputTypes", selectedModes);

      const response = await axiosPublic.post(
          '/api/File/generate', 
          fd
      )

      console.log(response.data);
  }

  const onCancelUpload = () => {
      abortRef.current?.abort(); 
  }

  return (
    
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
                {fileTypes.map((type) => {
                    var isSelected = selectedModes.includes(type.id);

                    return (
                        <button
                            key={type.id}
                            type="button"
                            onClick={() => {
                              toggleSelected(type.id)
                            }}
                            className={`bg-white border-1 rounded-lg p-4 cursor-pointer text-left transition-all hover:shadow-md hover:-translate-y-0.5 ${
                                isSelected ? "border-blue-600 shadow-sm" : "border-gray-300"
                            }`}
                        >
                            <div className="flex items-start gap-3">
                                <span
                                    className={`mt-0.5 w-[18px] h-[18px] rounded-full border-1 flex items-center justify-center transition-all ${
                                        isSelected ? "border-blue-600" : "border-gray-400"
                                    }`}
                                >
                                    {isSelected && <span className="w-2.5 h-2.5 rounded-full bg-blue-600" />}
                                </span>

                                <div>
                                    <div className="font-semibold text-base text-black">{type.title}</div>
                                    <div className="mt-1 text-xs text-gray-500">{type.desc}</div>
                                </div>
                            </div>
                        </button>
                    );
                })}
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
              <div key={file.id} className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg transition-all hover:shadow-md cursor-pointer">
                <div className="flex items-center justify-center">
                  <div className="w-10 h-10 flex items-center justify-center bg-blue-50 rounded-lg">
                    <FileCheck color="#3b82f6" size={20} />
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
        </section>
      </main>
  );
};

export default Dashboard;
