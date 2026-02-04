import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig, AxiosRequestConfig } from 'axios';

const rawBase = import.meta.env.VITE_API_URL;
const baseURL = rawBase ? rawBase.replace(/\/$/, '') : '/api';
const enableApiTiming = import.meta.env.DEV || import.meta.env.VITE_DEBUG_API_TIMING === 'true';

type TimedConfig = InternalAxiosRequestConfig & {
  metadata?: {
    startTimeMs: number;
  };
};

// Cloud backend (e.g. Azure) can be slow on cold start; use 60s to avoid false timeouts
const service: AxiosInstance = axios.create({
  baseURL,
  timeout: 60000, // 60s，兼容海外/跨区 API（如 Azure）较慢的情况
});

service.interceptors.request.use(
  (config: TimedConfig) => {
    if (enableApiTiming) {
      config.metadata = { startTimeMs: performance.now() };
    }

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
    if (enableApiTiming) {
      const cfg = response.config as TimedConfig;
      const start = cfg.metadata?.startTimeMs;
      if (typeof start === 'number') {
        const durationMs = Math.round(performance.now() - start);
        const method = (cfg.method || 'GET').toUpperCase();
        const url = `${cfg.baseURL ?? ''}${cfg.url ?? ''}`;
        // eslint-disable-next-line no-console
        console.info(`[API] ${method} ${url} ${durationMs}ms`);
      }
    }
    const res = response.data;
    return res;
  },
  (error: AxiosError) => {
    const cfg = (error.config || {}) as TimedConfig;
    const method = ((cfg as any).method || 'GET').toUpperCase();
    const url = `${(cfg as any).baseURL ?? ''}${(cfg as any).url ?? ''}`;
    const start = cfg.metadata?.startTimeMs;
    const durationMs = typeof start === 'number' ? Math.round(performance.now() - start) : undefined;

    console.error('Request Error:', {
      method,
      url,
      durationMs,
      code: (error as any).code,
      message: error.message,
      status: error.response?.status,
      data: error.response?.data,
    });

    // Helpful hint for timeouts
    if ((error as any).code === 'ECONNABORTED' && /timeout/i.test(error.message)) {
      console.warn(`[API] timeout after ${cfg.timeout ?? 'unknown'}ms: ${method} ${url}`);
    }
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
