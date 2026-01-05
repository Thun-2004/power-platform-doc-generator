import './styles/App.css'
import Header from './layouts/header'
import Dashboard from './pages/Dashboard'
import Sidebar from './layouts/sidebar'

function App() {
  return (
    <>
      <div className="h-screen bg-[#F9FAFC] flex flex-col overflow-hidden">
        <Header />
        <div className="flex flex-1 overflow-hidden">
          <Sidebar/>
          <Dashboard />
        </div>
      </div>

      {/* <div className="">
        <h1>Hello</h1>
      </div> */}

      {/* <header className="logo-header">
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
      </div> */}

      {/* <div className="sidebar">
        <Sidebar />
      </div> */}
  
    </>
  )
}

export default App
