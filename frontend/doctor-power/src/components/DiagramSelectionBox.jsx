import React, { useState } from "react";
import { Info } from "lucide-react";
import "../styles/App.css";

const DiagramSelectionBox = ({
  type,
  selectedModes,
  toggleSelected,
  charLimit,
  onOpenExamplePreview,
  hasExampleDoc,
  promptPlaceholder = 'e.g. Add brief instructions for this output',
}) => {
  const isSelected = selectedModes.includes(type.id);
  const [promptContent, setPromptContent] = useState("");

  return (
    <div
      className={`w-full bg-white border rounded-lg overflow-hidden text-left transition-all hover:shadow-md hover:-translate-y-0.5 ${
        isSelected ? "border-blue-600 shadow-sm" : "border-gray-300"
      }`}
    >
      <div className="flex w-full flex-col md:flex-row md:items-stretch">
        {/* Output type — clickable to toggle selection */}
        <div
          role="button"
          tabIndex={0}
          onClick={() => toggleSelected(type.id)}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              toggleSelected(type.id);
            }
          }}
          className="flex-1 min-w-0 p-2 sm:p-3 md:p-3 cursor-pointer outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-blue-500/40"
        >
          <div className="flex items-start gap-2 sm:gap-2 md:gap-2 lg:gap-2">
            <span
              className={`mt-0.5 shrink-0 w-[18px] h-[18px] min-w-[18px] min-h-[18px] rounded-full border border-gray-400 flex items-center justify-center transition-all ${
                isSelected ? "border-blue-600" : "border-gray-400"
              }`}
            >
              {isSelected && (
                <span className="w-2.5 h-2.5 min-w-[10px] min-h-[10px] rounded-full bg-blue-600 shrink-0" />
              )}
            </span>

            <div className="min-w-0 flex-1">
              <div className="flex items-start justify-between gap-2">
                <div className="font-semibold text-sm sm:text-sm md:text-base text-black min-w-0">
                  {type.title}
                </div>
                {hasExampleDoc && typeof onOpenExamplePreview === 'function' && (
                  <button
                    type="button"
                    className="shrink-0 p-1 rounded-md text-gray-400 hover:text-blue-600 hover:bg-blue-50 focus:outline-none focus:ring-2 focus:ring-blue-500/40"
                    aria-label={`Preview example for ${type.title}`}
                    title="Preview example output"
                    onClick={(e) => {
                      e.stopPropagation();
                      onOpenExamplePreview(type.id);
                    }}
                  >
                    <Info size={18} strokeWidth={2} />
                  </button>
                )}
              </div>
              <div className="mt-1 text-[11px] sm:text-xs text-gray-500 leading-snug">
                {type.desc}
              </div>
            </div>
          </div>
        </div>

        {/* Separator: horizontal bar + | on small screens, | between columns on md+ */}
        <div
          className="flex md:hidden items-center gap-2 px-2 sm:px-3 text-gray-300 shrink-0"
          aria-hidden="true"
        >
          <span className="flex-1 border-t border-gray-200" />
          <span className="text-sm font-light select-none">|</span>
          <span className="flex-1 border-t border-gray-200" />
        </div>
        <div
          className="hidden md:flex shrink-0 flex-col items-center self-stretch py-4 md:py-5 px-2 box-border"
          aria-hidden="true"
        >
          <div className="w-px flex-1 min-h-12 bg-gray-200 rounded-full" />
        </div>

        {/* Additional prompt */}
        <div
          className={`flex-1 min-w-0 p-2 sm:p-3 md:p-3 ${
            isSelected ? "opacity-100" : "opacity-50"
          }`}
        >
          <label className="flex mb-1 sm:mb-1.5 md:mb-2 text-xs sm:text-xs md:text-sm font-small justify-between gap-2 text-gray-600">
            Additional Prompt for {type.title}
            <span id={type.id + "-charcount"}>
              {promptContent.length}/{charLimit}
            </span>
          </label>
          <textarea
            value={promptContent}
            id={type.id}
            maxLength={charLimit}
            onChange={(e) => setPromptContent(e.target.value)}
            className="bg-gray-50 border border-default-medium text-heading text-xs sm:text-xs md:text-sm rounded-md focus:ring-brand focus:border-brand w-full px-1.5 py-2 md:py-2.5 shadow-xs placeholder:text-body"
            placeholder={promptPlaceholder}
          />
        </div>
      </div>
    </div>
  );
};

export default DiagramSelectionBox;
