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
            className={`bg-white border-1 rounded-lg p-2 sm:p-3 md:p-3 lg:p-3 cursor-pointer text-left transition-all hover:shadow-md hover:-translate-y-0.5 ${
                isSelected ? "border-blue-600 shadow-sm" : "border-gray-300"
            }`}
        >
            <div className="flex items-start gap-2 sm:gap-2 md:gap-2 lg:gap-2">
                <span
                    className={`mt-0.5 shrink-0 w-[18px] h-[18px] min-w-[18px] min-h-[18px] rounded-full border border-gray-400 flex items-center justify-center transition-all ${
                        isSelected ? "border-blue-600" : "border-gray-400"
                    }`}
                >
                    {isSelected && <span className="w-2.5 h-2.5 min-w-[10px] min-h-[10px] rounded-full bg-blue-600 shrink-0" />}
                </span>

                <div>
                    <div className="font-semibold text-sm sm:text-sm md:text-base text-black">{type.title}</div>
                    <div className="mt-1 text-[11px] sm:text-xs text-gray-500 leading-snug">{type.desc}</div>
                </div>
            </div>
        </button>

            <button className={`bg-white border-1 rounded-lg p-2 sm:p-3 md:p-3 lg:p-3 cursor-pointer text-left transition-all hover:shadow-md hover:-translate-y-0.5 ${
                (isSelected) ? "border-blue-600 shadow-sm opacity-100" : "border-gray-300 opacity-50"
            }`}>
            <label className="block mb-1 sm:mb-1.5 md:mb-2 text-xs sm:text-xs md:text-sm font-small flex justify-between text-gray-600">
              Additional Prompt for {type.title}
              <span id={type.id + "-charcount"}>{promptContent.length}/{charLimit}</span>
            </label>
            <textarea
              value={promptContent}
              id={type.id}
              maxLength={charLimit}
              onChange={e => setPromptContent(e.target.value)}
              className="bg-gray-50 border border-default-medium text-heading text-xs sm:text-xs md:text-sm rounded-md focus:ring-brand focus:border-brand w-full px-1.5 py-2 md:py-2.5 shadow-xs placeholder:text-body"
              placeholder="Additional prompt"
            />
            </button>

        </>
    );
};
export default DiagramSelectionBox;
                