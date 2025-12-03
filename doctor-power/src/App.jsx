import { useState } from 'react'
import dpLogo from './assets/dp-logo.png'
import viteLogo from '/vite.svg'
import './App.css'
import Upload from './Upload' 

function App() {
  const [count, setCount] = useState(0)

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
      
      <div className="card">
        <button onClick={() => setCount((count) => count +1)}>
          count is {count}
        </button>
      </div>
      <Upload />
    
    </>
  )
}

export default App
