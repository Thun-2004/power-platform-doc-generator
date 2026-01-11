import React, { useState, useRef } from "react";
import uploadFile from "../api/file";

const fileTypes = [
    { id: "er", title: "ER diagram", desc: "Set fixed price for people to buy your product instantly" },
    { id: "ui", title: "UI-Hierarchy flow", desc: "Set fixed price for people to buy your product instantly" },
    { id: "program", title: "Program flow", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram4", title: "diagram4", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram5", title: "diagram5", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram6", title: "diagram6", desc: "Set fixed price for people to buy your product instantly" },
];

const OutputSelect = () => {
    //FIXME: finish axios private for this
    // const axiosPrivate = useAxiosPrivate();

    const [selected, setSelected] = useState([]);
    const [selectedFile, setSelectedFile] = useState(null);
    const [selectedModes, setSelectedModes] = useState([]);
    const [progress, setProgress] = useState(0);
    const [isUploading, setIsUploading] = useState(false);

    const abortRef = useRef(null);  //like useState but not rerender 

    const toggleSelected = (id) => {
        setSelected((prevSelected) =>
            prevSelected.includes(id)
                ? prevSelected.filter((item) => item !== id)
                : [...prevSelected, id]                      
        );
        if (!selectedModes.includes(id)) {
            setSelectedModes([...selectedModes, id]);
        }else{
            setSelectedModes(selectedModes.filter((item) => item !== id)); 
        }
        console.log("Selected modes:", selectedModes);
    };

    const onPickFile = (e) => {
        const f = e.target.files?.[0] ?? null;
        setFile(f);
        setProgress(0);
    };

    //FIXME: might need to change in case multiple files are allowed
    const onRemoveFile = () => {
        setFile(null); 
        setProgress(0); 
    };

    const onGenerateOutputFile = () => {
        if(!file) return; 

        const fd = new FormData(); 
        fd.append("file", file); 
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

    const onCancelUpload = () => {
        abortRef.current?.abort(); 
    }

    return (
        <div className="w-full">
            <h2 className="text-title">Select output file types</h2>

            <div className="grid grid-cols-2 gap-4">
                {fileTypes.map((type) => {
                    const isSelected = selected.includes(type.id);

                    return (
                        <button
                            key={type.id}
                            type="button"
                            onClick={() => toggleSelected(type.id)}
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
                <button className="btn-theme">Generate</button>
            </div>
        </div>
    );
}




export default OutputSelect;