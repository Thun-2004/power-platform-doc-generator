import React, { useState } from "react";
import { useNavigate } from 'react-router-dom';
import { House, Bookmark, User, Menu } from 'lucide-react';
import "../styles/App.css";

const Sidebar = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const navigate = useNavigate();
  const [activeMenu, setActiveMenu] = useState('home');

  return (
    <aside className={`w-64 bg-gray-100 p-6 flex flex-col gap-3 flex-shrink-0 transition-all ${isSidebarOpen ? '' : ''}`}>
        <div className="flex items-center">
          <button 
            className="bg-transparent border-none cursor-pointer p-2 flex items-center justify-center"
            onClick={() => setIsSidebarOpen(!isSidebarOpen)}
          >
            <Menu size={20} />
          </button>
          <span className="text-title mt-3">Menu</span>
        </div>
        <nav className="flex flex-col gap-1 text-sm font-medium">
          <div 
            className={`flex items-center gap-3 px-4 py-3 rounded-lg cursor-pointer transition-all ${
              activeMenu === 'home'
                ? 'bg-blue-600 text-white'
                : 'text-gray-600 hover:bg-gray-200'
            }`}
            onClick={() => {
              setActiveMenu('home')
              navigate('/', { replace: true });
            }}
          >
            <House size={20} />
            <span className="text-menu">Home</span>
          </div>
          <div 
            className={`flex items-center gap-3 px-4 py-3 rounded-lg cursor-pointer transition-all ${
              activeMenu === 'collections' 
                ? 'bg-blue-600 text-white' 
                : 'text-gray-600 hover:bg-gray-200'
            }`}
            onClick={() => {
              setActiveMenu('collections')
              navigate('/mycollection', { replace: true });
            }}
          >
            <Bookmark size={20}/>
            <span className="text-menu">Your collections</span>
          </div>
          <div 
            className={`flex items-center gap-3 px-4 py-3 rounded-lg cursor-pointer transition-all ${
              activeMenu === 'profile' 
                ? 'bg-blue-600 text-white' 
                : 'text-gray-600 hover:bg-gray-200'
            }`}
            onClick= {() => {
              setActiveMenu('userprofile')
              navigate('/userprofile', { replace: true });
            }}
          >
            <User size={20}/>
            <span className="text-menu">User Profile</span>
          </div>
        </nav>
      </aside>
  );
}

export default Sidebar;