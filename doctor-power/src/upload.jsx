import React from "react";
import "./App.css"

const UploadBox = () => {
    return (
        <div className="upload-container">
            <h2 className="upload-title">Upload File</h2>

            <div className="upload-box">
                <div className="upload-content">

                    <label className="upload-button">
                        Browse File
                        <input
                            type="file"
                            style={{ display: "none" }}
                        />
                    </label>
                </div>
            </div>
        </div>
    );
};

export default UploadBox;
