import { useState } from 'react'
import dpLogo from './assets/dp-logo.png'
import './App.css'
import Upload from './Upload'
import FileOutput from './fileOutput'
import Sidebar from './sidebar' 
// import FileUpload from './fileUpload'

function App() {

  return (
    <>
      <header className="logo-header">
          <img src={dpLogo} className="logo" alt="Doctor Power Logo" />
          <h1 className="logo-text">octor Power</h1>
      </header>

      <div className="sign-up-login">
        <button className="auth-button">Login</button>
        <button className="auth-button">Sign Up</button>
      </div>

      <div className="file-upload">
        <Upload />
      </div>

      <div className="file-output">
        <FileOutput />
      </div>

      <div className="sidebar">
        <Sidebar />
      </div>
  
    </>
  )
}

export default App
