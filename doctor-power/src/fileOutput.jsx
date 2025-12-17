import React, { useState } from "react";
import "./App.css"

const fileTypes = [
    { id: "er", title: "ER diagram", desc: "Set fixed price for people to buy your product instantly" },
    { id: "ui", title: "UI-Hierarchy flow", desc: "Set fixed price for people to buy your product instantly" },
    { id: "program", title: "Program flow", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram4", title: "diagram4", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram5", title: "diagram5", desc: "Set fixed price for people to buy your product instantly" },
    { id: "diagram6", title: "diagram6", desc: "Set fixed price for people to buy your product instantly" },
];

const OutputSelect = () => {
    const [selected, setSelected] = useState([]);
    const toggleSelected = (id) => {
        setSelected((prevSelected) =>
            prevSelected.includes(id)
                ? prevSelected.filter((item) => item !== id)
                : [...prevSelected, id]                      
        );
    };

    return (
        <div className="file-type-selector">
            <h2 className="fts-heading">Select output file types</h2>

            <div className="fts-grid">
                {fileTypes.map((type) => {
                    const isSelected = selected.includes(type.id);

                    return (
                        <button
                            key={type.id}
                            type="button"
                            onClick={() => toggleSelected(type.id)}
                            className={`fts-card ${isSelected ? "fts-card--selected" : ""}`}
                        >
                            <div className="fts-card-row">
                                <span
                                    className={`fts-radio ${isSelected ? "fts-radio--selected" : ""}`}
                                >
                                    {isSelected && <span className="fts-radio-inner" />}
                                </span>

                                <div>
                                    <div className="fts-title">{type.title}</div>
                                    <div className="fts-desc">{type.desc}</div>
                                </div>
                            </div>
                        </button>
                    );
                })}
            </div>
        </div>
    );
}




export default OutputSelect;