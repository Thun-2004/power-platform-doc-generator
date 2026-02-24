import React, { useState, useRef, useEffect } from "react";
import { FileCheck,Trash2 } from 'lucide-react';
import axiosPublic from "../api/axios";
import uploadFile from "../api/file";
import DocumentPreviewModal from "../components/DocumentPreviewModal";
import DiagramSelectionBox from "../components/DiagramSelectionBox";
import DocumentOutputPreview from "../components/DocumentOutputPreview";

//temp
import axios from 'axios';


const Dashboard = () => {

  const fileTypes = [
    { id: "overview", title: "Overview", desc: "Dummy text for generating an ER diagram"},
    { id: "workflows", title: "Workflows", desc: "Dummy text for generating a UI-hierarchy"},
    { id: "faq", title: "Frequently Asked Questions", desc: "Dummy text for generating a program-flow"},
    { id: "diagrams", title: "Diagrams", desc: "Dummy option for assigning some AI task"},
    { id: "erd", title: "ER diagram", desc: "Dummy text for generating an ER diagram"},
    { id: "environment-variables", title: "Environment variables", desc: "Dummy option for generating environment variables"}
  ];

  const statusSymbols = {
    'Pending': "⭘ Uploading Files",
    'Processing': "🔘 File is being processed by LLM",
    'Completed': "✅ Output should be produced",
    'Failed': "❌ request failed",
  }
  
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
 
  const delay = ms => new Promise(res => setTimeout(res, ms)); //call await delay(n) to wait n ms
  const tickLength = 100; //Tick length for calling API to check job status


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
        const selectedModesClone = new Array();
        selectedModes.forEach((m) => {
          selectedModesClone.push(m);
        }) //Lock the modes that we work with

        setIsUploading(true);

        //Get prompts from selected output types so that they can be passed to the backend
        let prompts = {}
        selectedModesClone.forEach((m) => prompts[m] = document.getElementById(m).value);

        console.log(selectedModesClone);
        console.log(prompts);

        let formData = new FormData();

      
        formData.append('File', selectedFile);
        formData.append('SelectedOutputTypes', selectedModes);
        // formData.append('additionalPrompts', prompts); //

        const response = await axiosPublic.post('/api/File/generate', formData)
 
        
        // //Retrieve data from API response
        let status = response.data.data['jobStatus'];
        let statusUrl = response.data.data['jobStatusUrl'];
        let outputApiUrls = response.data.data['outputFilesMetas']; //list of APIs to call that generate the document
        selectedModesClone.forEach((m) => console.log(outputApiUrls[m]));


        // // let i = 0;
        while(status != 'Failed' && status != 'Completed'){
          await delay(tickLength);
          status = (await axiosPublic.get(statusUrl)).data.data['jobStatus'];
          console.log(status);
          updateStatusSymbol(status);
        //   // i++;
        }
        // var status;
        // status = 'Completed';
        if (status == 'Failed'){
          console.log('Failed');
          updateStatusSymbol(status)
          // TODO Figure out how to display the retryOutputButton here
        }
        else {
          //Call the outputAPI
            
          selectedModesClone.forEach(async (m) =>  
          {
            console.log(outputApiUrls[m])
            // const response = await axiosPublic.get("api/File/getDocument/" + m, {responseType: 'blob'});
            const response = await axiosPublic.get(outputApiUrls[m], {responseType: 'blob'});
            console.log(response);
            const contentDisposition = response.headers['content-disposition'] || '';
            console.log(contentDisposition);

            let fileName = "generated_document";
            // let fileName = "generated_document";
            console.log(fileName)
            const fileNameMatch = /filename=(?:"?)([^;\"]+)/i.exec(contentDisposition);
            console.log(fileNameMatch);
            if (fileNameMatch && fileNameMatch[1]) fileName = fileNameMatch[1].replace(/\"/g, '');
            console.log(fileName);
            const blob = new Blob([response.data], { type: response.data.type || 'application/octet-stream' });
            const url = URL.createObjectURL(blob);


            // ensure fileName has extension, fallback using blob.type
            if (!fileName.includes('.')) {
              const mime = blob.type;
              const mimeToExt = {
                'application/vnd.openxmlformats-officedocument.wordprocessingml.document': '.docx',
                'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': '.xlsx',
                'application/pdf': '.pdf',
                'application/zip': '.zip'
              };
              if (mimeToExt[mime]) fileName += mimeToExt[mime];
            }
            // console.log(mime);

            setOutputFiles(prev => [
              ...prev,
              {
                id: Date.now(),
                name: fileName,
                url,
                blob
              }
            ]);
            }
            
          )
          
        }





      } catch (err) {
        console.error('Failed to fetch document', err);
      } finally {
        setIsUploading(false);
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
                <button onClick={onGenerateOutputFile} className="btn-theme">Generate</button>
                <h2 id="jobStatus"></h2>
            </div>
        </div>
        </section>

        {/* Output Section */}
        <section className="mt-8">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-title">Output(2)</h2>
            <button className="btn-theme">Download all(2)</button>
          </div>
          <div id="file-display" className="flex flex-col gap-4">
              <DocumentOutputPreview outputFiles={outputFiles} setPreviewFile={setPreviewFile}/>
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
