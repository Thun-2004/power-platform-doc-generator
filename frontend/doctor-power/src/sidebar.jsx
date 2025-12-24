import React, { useState } from "react";
import "./App.css";

const Sidebar = () => {
   const [isOpen, setIsOpen] = useState(false);

  return (
    <aside
      className={`floating-sidebar ${
        isOpen ? "floating-sidebar--open" : ""
      }`}
    >
      <button
        className="floating-sidebar__toggle"
        onClick={() => setIsOpen((prev) => !prev)}
      >
        <span className="hamburger">
          <span></span>
          <span></span>
          <span></span>
        </span>
        <span className="floating-sidebar__label">Menu</span>
      </button>

      <div className="floating-sidebar__content">
        
        <p>Stuff</p>
      </div>
    </aside>
  );
}

export default Sidebar;