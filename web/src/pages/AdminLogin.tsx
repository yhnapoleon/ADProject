import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './AdminLogin.css';
import request from '../utils/request';

const AdminLogin: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!username || !password) {
      setError('Please enter both username and password');
      return;
    }

    try {
      // 调用管理员登录API
      // 注意：request 的响应拦截器已经返回了 response.data，所以这里直接是响应数据
      const response: any = await request.post('/admin/auth/login', {
        username,
        password,
      });

      // 保存token和用户信息
      if (response?.accessToken || response?.token) {
        const token = response.accessToken || response.token;
        localStorage.setItem('adminToken', token);
        localStorage.setItem('token', token); // 同时保存为通用token
        localStorage.setItem('adminAuthenticated', 'true');
        localStorage.setItem('adminUsername', response?.admin?.username || username);
        
        // 跳转到管理面板
        navigate('/admin');
      } else {
        setError('Login failed: No token received');
      }
    } catch (error: any) {
      console.error('Login error:', error);
      setError(
        error.response?.data?.error || 
        error.response?.data?.message || 
        error.message || 
        'Login failed. Please check your credentials.'
      );
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-header">
          <h1 className="login-logo">EcoLens</h1>
          <p className="login-subtitle">Admin Portal</p>
        </div>
        
        <form className="login-form" onSubmit={handleSubmit}>
          <h2 className="login-title">Admin Login</h2>
          
          {error && (
            <div className="error-message">
              {error}
            </div>
          )}

          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter your username"
              className="form-input"
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              className="form-input"
            />
          </div>

          <button type="submit" className="login-button">
            Login In
          </button>
        </form>
      </div>
    </div>
  );
};

export default AdminLogin;
