import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import AdminLogin from './pages/AdminLogin';
import AdminSidebar from './components/AdminSidebar';
import AdminDashboard from './pages/AdminDashboard';
import AdminUserList from './pages/AdminUserList';
import AdminEmissionFactors from './pages/AdminEmissionFactors';
import AdminCommunityAnalytics from './pages/AdminCommunityAnalytics';
import AdminSettings from './pages/AdminSettings';
import ProtectedRoute from './components/ProtectedRoute';
import './App.css';

const App: React.FC = () => {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<AdminLogin />} />
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <div className="app">
                <AdminSidebar />
                <div className="main-content">
                  <Routes>
                    <Route path="/" element={<AdminDashboard />} />
                    <Route path="/users" element={<AdminUserList />} />
                    <Route path="/emission-factors" element={<AdminEmissionFactors />} />
                    <Route path="/community-analytics" element={<AdminCommunityAnalytics />} />
                    <Route path="/settings" element={<AdminSettings />} />
                  </Routes>
                </div>
              </div>
            </ProtectedRoute>
          }
        />
      </Routes>
    </Router>
  );
};

export default App;
