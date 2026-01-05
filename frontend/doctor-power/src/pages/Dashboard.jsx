import React, { useState } from "react";
import { FileCheck } from 'lucide-react';
import OutputSelect from '../layouts/fileOutput';
import Sidebar from '../layouts/sidebar';

const Dashboard = () => {
  
  const outputFiles = [
    { id: 1, name: "Client1-ER-diagram.pdf", type: "pdf" }
  ];

  return (
    
      <main className="flex-1 p-8 bg-white m-4 rounded-xl shadow-sm overflow-y-auto">
        {/* Upload File Section */}
        <section className="mb-8">
          <h2 className="text-title">Upload File</h2>
          <div className="w-full min-h-[200px] border-2 border-dashed border-gray-300 rounded-xl bg-gray-50 flex justify-center items-center p-8">
            <div className="text-center flex flex-col items-center gap-4">
              <p className="text-base text-gray-600 m-0">Drag and drop your file here</p>
              <p className="text-sm text-gray-400 m-0">Max 120 MB, PNG, JPEG, MP3, MP4</p>
              <label className="btn-theme">
                Browse File
                <input
                  type="file"
                  accept=".png,.jpeg,.jpg,.mp3,.mp4"
                  className="hidden"
                  onChange={(e) => {
                    if (e.target.files && e.target.files[0]) {
                      const file = e.target.files[0];
                      console.log('Selected file:', file.name);
                    }
                  }}
                />
              </label>
            </div>
          </div>
        </section>

        {/* Select Output File Types Section */}
        <section className="mb-8">
          <OutputSelect />
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
