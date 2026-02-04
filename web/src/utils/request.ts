import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig, AxiosRequestConfig } from 'axios';

const rawBase = import.meta.env.VITE_API_URL;
const baseURL = rawBase ? rawBase.replace(/\/$/, '') : '/api';

const service: AxiosInstance = axios.create({
  baseURL,
  timeout: 30000,
});

service.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Normalize to avoid duplicated /api when baseURL already contains /api
    if (config.url && baseURL && baseURL.endsWith('/api') && config.url.startsWith('/api')) {
      config.url = config.url.replace(/^\/api/, '');
    }

    // 从localStorage获取token并添加到请求头
    const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
    if (token) {
      config.headers = config.headers || {};
      config.headers.Authorization = `Bearer ${token}`;
    }
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
    // 处理401未授权错误，清除token
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('adminToken');
      localStorage.removeItem('adminAuthenticated');
      // 注意：不再在这里自动跳转，让调用方决定如何处理
      // 这样可以避免在上传图片等操作时意外跳转
    }
    return Promise.reject(error);
  }
);

const request = {
  get: <T = any>(url: string, config?: AxiosRequestConfig) => service.get(url, config) as unknown as Promise<T>,
  post: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => service.post(url, data, config) as unknown as Promise<T>,
  put: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => service.put(url, data, config) as unknown as Promise<T>,
  delete: <T = any>(url: string, config?: AxiosRequestConfig) => service.delete(url, config) as unknown as Promise<T>,
  // expose the raw axios instance for advanced use if needed
  raw: service,
};

export default request;
