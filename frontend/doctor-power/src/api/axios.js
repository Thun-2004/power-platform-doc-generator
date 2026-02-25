import axios from 'axios';

const BASE_URL = import.meta.env.VITE_POWERDOCU_BACKEND_URL || 'http://localhost:5280';

export const axiosPublic = axios.create({
    baseURL: BASE_URL
})

export const axiosPrivate = axios.create({
    baseURL: BASE_URL,
    withCredentials: true
})

export default axiosPublic;