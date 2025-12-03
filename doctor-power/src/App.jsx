import { useState } from 'react'
import dpLogo from './assets/dp-logo.png'
import viteLogo from '/vite.svg'
import './App.css'

function App() {
  const [count, setCount] = useState(0)

  return (
    <>
      <div className='Images'>
          <img src={dpLogo} className="logo react" alt="React logo" />
      </div>
      <h1>Welcome to doctor Power!</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count +1)}>
          count is {count}
        </button>
      </div>
      <p className="read-the-docs">
        To do:
        <ul>
          <li>Add file upload system</li>
          <li>Get the basic UI done</li>
        </ul>
      </p>
    </>
  )
}

export default App
