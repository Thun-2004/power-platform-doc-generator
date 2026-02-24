import {
  BrowserRouter as Router,
  Routes,
  Route
} from 'react-router-dom'

import './styles/App.css'
import Header from './layouts/header'
import Dashboard from './pages/Dashboard'
import Sidebar from './layouts/sidebar'
import MyCollection from './pages/MyCollection'
import UserProfile from './pages/UserProfile'


function App() {
  return (
    <Router>
      
        <div className="h-screen bg-[#F9FAFC] flex flex-col overflow-hidden">
          <Header />
          <div className="flex flex-1 overflow-hidden">
            
            <Sidebar/>
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route path="/mycollection" element={<MyCollection />} />
              <Route path="/userprofile" element={<UserProfile />} />
            </Routes>
           
          </div>
        </div>
    </Router>
    
  )
}

export default App
