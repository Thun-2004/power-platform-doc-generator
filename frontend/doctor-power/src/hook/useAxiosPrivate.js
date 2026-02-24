import { axiosPrivate } from '../api/axios';
import { useEffect } from 'react';
import useRefreshToken from './useRefreshToken';
import useAuth from './useAuth';


//FIXME: add useRefreshToken and useAuth dependencies
const useAxiosPrivate = () => {
    const refresh = useRefreshToken();
    const { auth } = useAuth();

    useEffect(() => {
        const requestIntercept = axiosPrivate.interceptors.request.use(
            (config) => {
                if (!config.headers['Authorization']) {
                    config.headers[
                        'Authorization'
                    ] = `Bearer ${auth?.accessToken}`;
                }
                return config;
            },
            (error) => Promise.reject(error)
        );

        const responseIntercept = axiosPrivate.interceptors.response.use(
            (response) => response,
            async (error) => {
                const prevReq = error?.config;
                if (error?.response?.status === 401 && !prevReq._retry) {
                    prevReq.sent = true;
                    const newAccessToken = await refresh();
                    prevReq.headers[
                        'Authorization'
                    ] = `Bearer ${newAccessToken.jwt_token}`;
                    auth.accessToken = newAccessToken.jwt_token;
                    return axiosPrivate(prevReq);
                }
                return Promise.reject(error);
            }
        );

        return () => {
            axiosPrivate.interceptors.request.eject(requestIntercept);
            axiosPrivate.interceptors.response.eject(responseIntercept);
        };
    }, [auth, refresh]);
    return axiosPrivate;
};

export default useAxiosPrivate;
