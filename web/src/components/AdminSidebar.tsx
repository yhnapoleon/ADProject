import React from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import './AdminSidebar.css';

const AdminSidebar: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  const menuItems = [
    { path: '/admin', label: 'Dashboard' },
    { path: '/admin/users', label: 'User List' },
    { path: '/admin/emission-factors', label: 'Emission Factors' },
    { path: '/admin/community-analytics', label: 'Community Analytics' },
    { path: '/admin/user-emissions', label: 'User Emissions' },
  ];

  const handleLogout = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    
    const confirmed = window.confirm('Are you sure you want to logout?');
    if (confirmed) {
      localStorage.removeItem('adminAuthenticated');
      localStorage.removeItem('adminUsername');
      navigate('/admin/login');
    }
  };

  return (
    <div className="sidebar">
      <div className="sidebar-header">
        <h1 className="sidebar-logo">EcoLens</h1>
        <p className="sidebar-subtitle">Admin Portal</p>
      </div>
      <nav className="sidebar-nav">
        {menuItems.map((item) => (
          <Link
            key={item.path}
            to={item.path}
            className={`sidebar-item ${location.pathname === item.path ? 'active' : ''}`}
          >
            {item.label}
          </Link>
        ))}
        <button 
          className="sidebar-item logout" 
          onClick={handleLogout}
          type="button"
        >
          Logout
        </button>
      </nav>
    </div>
  );
};

export default AdminSidebar;
