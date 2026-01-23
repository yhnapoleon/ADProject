import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';

const service: AxiosInstance = axios.create({
  baseURL: (import.meta as any).env?.VITE_API_URL || '/api',
  timeout: 10000,
});

service.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // TODO: Add token logic here
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

service.interceptors.response.use(
  (response) => {
    const res = response.data;
    return res;
  },
  (error: AxiosError) => {
    console.error('Request Error:', error);
    return Promise.reject(error);
  }
);

export default service;
