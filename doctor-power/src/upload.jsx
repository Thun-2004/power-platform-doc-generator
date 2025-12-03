import React, { useState } from "react";
import "./App.css"

const UploadBox = () => {
    const [selectedFile, setSelectedFile] = useState(null);
    // const [uploadStatus, setUploadStatus] = useState('');

    const handleFileSelect = (event) => {
        const file = event.target.files[0];
        setSelectedFile(file)    };



    return (
        <div className="upload-container">
            <h2 className="upload-title">Upload File</h2>
            <div className="upload-box">
                <div className="upload-content">
                    <label id="upload-button">
                        Browse File
                        <input
                            type="file"
                            accept=".zip"
                            style={{ display: "none" }}
                            onChange={handleFileSelect}
                        />
                    </label>
                </div>
            </div>
            <div className="upload-output">
                {selectedFile && (
                    <div>
                        <p>File name: {selectedFile.name}</p>
                        <p>File type: {selectedFile.type}</p>
                        <p>File size: {(selectedFile.size / 1024 / 1024).toFixed(2)} MB</p>
                    </div>
                )}
            </div>
        </div>

    )

};


export default UploadBox;
