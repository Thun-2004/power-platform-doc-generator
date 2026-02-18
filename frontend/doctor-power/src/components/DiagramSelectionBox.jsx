import React, { useState } from "react";
import { useNavigate } from 'react-router-dom';
import { House, Bookmark, User, Menu } from 'lucide-react';
import "../styles/App.css";


const DiagramSelectionBox = ({type, selectedModes, toggleSelected}) => {
    var isSelected = selectedModes.includes(type.id);

    return (
        <>
        <button 
            key={type.id}
            type="button"
            onClick={() => {
                toggleSelected(type.id)
            }}
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

            <button className={`bg-white border-1 rounded-lg p-4 cursor-pointer text-left transition-all hover:shadow-md hover:-translate-y-0.5 ${
                (isSelected) ? "border-blue-600 shadow-sm opacity-100" : "border-gray-300 opacity-50"
            }`}>
            <label className="block mb-2.5 text-sm font-small text-gray-600">Additional Prompt for {type.title}</label>
            <textarea id={type.id} className="bg-gray-50 border border-default-medium text-heading text-sm rounded-md focus:ring-brand focus:border-brand  w-full px-1 py-2.5 shadow-xs placeholder:text-body" placeholder="Additional prompt" />
            </button>

        </>
    );
};
export default DiagramSelectionBox;
                