import dpLogo from './assets/dp-logo.png'
import './App.css'
import Upload from './upload'
import FileOutput from './fileOutput'
import Sidebar from './sidebar' 
// import FileUpload from './fileUpload'
import Dashboard from './Main/Dashboard'

function App() {
  return (
    <>
      <div className="h-screen bg-[#F9FAFC] flex flex-col overflow-hidden">
        <header className="flex justify-between items-center px-8 py-2.5 bg-white shadow-sm flex-shrink-0">
          <div className="flex items-end">
            <img src={dpLogo} className="h-19 p-2" alt="Doctor Power Logo" />
            <div className="flex flex-col items-start justify-end mb-2">
              <p className="text-xl font-bold text-blue-600 m-0">octor Power</p>
              <p className="text-sm text-gray-500 m-0 -mt-1">Documentation Generator</p>
            </div>
          </div>
          <div className="flex gap-4">
            <button className="w-30 h-10 bg-blue-600 text-white border-none rounded-md text-base cursor-pointer flex justify-center items-center transition-all hover:brightness-110">Login</button>
            <button className="w-30 h-10 bg-blue-600 text-white border-none rounded-md text-base cursor-pointer flex justify-center items-center transition-all hover:brightness-110">Signin</button>
          </div>
        </header>
        <Dashboard />
      </div>

      {/* dont delete this part */}

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
