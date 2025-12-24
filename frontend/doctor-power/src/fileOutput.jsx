import React, { useState } from "react";

const fileTypes = [
    { id: "er", title: "ER diagram", desc: "Set fixed price for people to buy your product instantly" },
    { id: "ui", title: "UI-Hierarchy flow", desc: "Set fixed price for people to buy your product instantly" },
    { id: "program", title: "Program flow", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram4", title: "diagram4", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram5", title: "diagram5", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram6", title: "diagram6", desc: "Set fixed price for people to buy your product instantly" },
];

// const OutputSelect = () => {
//     const [selected, setSelected] = useState([]);
//     const toggleSelected = (id) => {
//         setSelected((prevSelected) =>
//             prevSelected.includes(id)
//                 ? prevSelected.filter((item) => item !== id)
//                 : [...prevSelected, id]                      
//         );
//     };

const OutputSelect = ({ onGenerate, isGenerating }) => {
    const [selected, setSelected] = useState([]);

    const toggleSelected = (id) => {
        setSelected((prev) =>
        prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
        );
    };
    

    return (
        <div className="w-full">
            <h2 className="text-xl font-semibold mb-3 text-gray-800">Select output file types</h2>

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
            <button 
                className="bg-blue-600 text-white px-4 py-2 rounded-md text-sm mt-4 font-medium cursor-pointer transition-all hover:brightness-110"
                onClick={() => 
                    isGenerating = true
                }
            >Generate documents</button>
            
        </div>
    );
}




export default OutputSelect;