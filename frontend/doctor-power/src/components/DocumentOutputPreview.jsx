import React from 'react';
import { FileCheck} from 'lucide-react';

import "../styles/App.css";


import RetryOutputButton from './RetryOutputButton';

const DocumentOutputPreview = ({outputFiles, setPreviewFile}) => {

    return (
    outputFiles.map((file) => 
            (
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
            )))

        }

export default DocumentOutputPreview;
