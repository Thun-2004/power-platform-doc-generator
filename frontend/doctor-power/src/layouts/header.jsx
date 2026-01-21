
// import dpLogo from './assets/dp-logo.png'
import dpLogo from '../assets/dp-logo.png'

const Header = () => {
    return (
        <header className="flex justify-between items-center px-8 py-2.5 bg-white shadow-sm flex-shrink-0">
            <div className="flex items-end">
            <img src={dpLogo} className="h-19 p-2" alt="Doctor Power Logo" />
            <div className="flex flex-col items-start justify-end mb-2">
                <p className="text-xl font-bold text-blue-600 m-0">octor Power</p>
                <p className="text-sm text-gray-500 m-0 -mt-1">Power Platform Documentation Generator</p>
            </div>
            </div>
            <div className="flex gap-4">
            <button className="w-30 h-10 btn-theme">Login</button>
            <button className="w-30 h-10 btn-theme">Signin</button>
            </div>
        </header>
    )
}

export default Header;