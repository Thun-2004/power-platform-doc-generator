import React, { useState } from "react";
import "./App.css"

const UploadBox = () => {
    const [selectedFile, setSelectedFile] = useState(null);
    // const [uploadStatus, setUploadStatus] = useState('');

    const handleFileSelect = (event) => {
        const file = event.target.files[0];
        setSelectedFile(file)    };

    return (
        <div className="upload-container-new">
            <div className="upload-drag-drop-new">
                <div className="upload-drag-content-new">
                    <p className="upload-hint-new">Drag and drop your file here</p>
                    <p className="upload-constraints-new">Max 120 MB, PNG, JPEG, MP3, MP4</p>
                    <label className="browse-button-new">
                        Browse File
                        <input
                            type="file"
                            accept=".png,.jpeg,.jpg,.mp3,.mp4"
                            style={{ display: "none" }}
                            onChange={handleFileSelect}
                        />
                    </label>
                </div>
            </div>
        </div>
    )

};


export default UploadBox;
