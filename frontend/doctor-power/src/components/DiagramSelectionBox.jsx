import React, { useState } from "react";
import { useNavigate } from 'react-router-dom';
import { House, Bookmark, User, Menu } from 'lucide-react';
import "../styles/App.css";


const DiagramSelectionBox = ({type, selectedModes, toggleSelected, charLimit}) => {
    var isSelected = selectedModes.includes(type.id);
    
    const [promptContent, setPromptContent] = useState('');

    return (
        <>
        <button 
            key={type.id}
            type="button"
            onClick={() => {
                toggleSelected(type.id)
            }}
            className={`bg-white border-1 rounded-lg p-3 sm:p-4 md:p-5 lg:p-6 cursor-pointer text-left transition-all hover:shadow-md hover:-translate-y-0.5 ${
                isSelected ? "border-blue-600 shadow-sm" : "border-gray-300"
            }`}
        >
            <div className="flex items-start gap-2 sm:gap-3 md:gap-3 lg:gap-4">
                <span
                    className={`mt-0.5 shrink-0 w-[18px] h-[18px] min-w-[18px] min-h-[18px] rounded-full border border-gray-400 flex items-center justify-center transition-all ${
                        isSelected ? "border-blue-600" : "border-gray-400"
                    }`}
                >
                    {isSelected && <span className="w-2.5 h-2.5 min-w-[10px] min-h-[10px] rounded-full bg-blue-600 shrink-0" />}
                </span>

                <div>
                    <div className="font-semibold text-base text-black">{type.title}</div>
                    <div className="mt-1 text-xs text-gray-500">{type.desc}</div>
                </div>
            </div>
        </button>

            <button className={`bg-white border-1 rounded-lg p-3 sm:p-4 md:p-5 lg:p-6 cursor-pointer text-left transition-all hover:shadow-md hover:-translate-y-0.5 ${
                (isSelected) ? "border-blue-600 shadow-sm opacity-100" : "border-gray-300 opacity-50"
            }`}>
            <label className="block mb-2 sm:mb-2.5 md:mb-3 text-xs sm:text-sm md:text-base font-small text-gray-600">Additional Prompt for {type.title}</label>
            <label className="block mb-2 sm:mb-2.5 md:mb-3 text-xs sm:text-sm md:text-sm font-small text-gray-600 justify-self-end"> <span id={type.id + "-charcount"}>{promptContent.length}</span>/{charLimit}</label>
            <textarea value={promptContent} id={type.id} maxLength={charLimit} onChange={e => setPromptContent(e.target.value)} className="bg-gray-50 border border-default-medium text-heading text-xs sm:text-sm md:text-base rounded-md focus:ring-brand focus:border-brand  w-full px-1 py-2.5 md:py-3 shadow-xs placeholder:text-body" placeholder="Additional prompt" />
            </button>

        </>
    );
};
export default DiagramSelectionBox;
                