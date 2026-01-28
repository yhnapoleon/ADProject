import { useEffect, useState } from 'react';
import { BrowserRouter, Routes, Route, Navigate, Outlet, useLocation } from 'react-router-dom';
import { ConfigProvider } from 'antd';
import MainLayout from './components/MainLayout';
import SplashScreen from './components/SplashScreen';
import Dashboard from './pages/Dashboard';
import Profile from './pages/Profile';
import Records from './pages/Records';
import Leaderboard from './pages/Leaderboard';
import AboutMe from './pages/AboutMe';
import AIAssistant from './pages/AIAssistant';
import Onboarding from './pages/Onboarding';
import LogMeal from './pages/LogMeal';
import LogTravel from './pages/LogTravel';
import LogUtility from './pages/LogUtility';
import TreePlanting from './pages/TreePlanting';
import Login from './pages/Login';
import Register from './pages/Register';
import AdminLogin from './pages/AdminLogin';
import AdminSidebar from './components/AdminSidebar';
import AdminDashboard from './pages/AdminDashboard';
import AdminUserList from './pages/AdminUserList';
import AdminEmissionFactors from './pages/AdminEmissionFactors';
import AdminCommunityAnalytics from './pages/AdminCommunityAnalytics';
import AdminSettings from './pages/AdminSettings';
import ProtectedRoute from './components/ProtectedRoute';
import './App.css';

function getIsLoggedIn() {
  try {
    return localStorage.getItem('isLoggedIn') === 'true';
  } catch {
    return false;
  }
}

const RequireUserAuth = () => {
  if (!getIsLoggedIn()) return <Navigate to="/login" replace />;
  return <Outlet />;
};

const RedirectIfAuthed = () => {
  if (getIsLoggedIn()) return <Navigate to="/dashboard" replace />;
  return <Outlet />;
};

// 动态设置页面标题的组件
const TitleUpdater: React.FC = () => {
  const location = useLocation();

  useEffect(() => {
    if (location.pathname.startsWith('/admin')) {
      document.title = 'EcoLens Admin Portal';
    } else {
      document.title = 'EcoLens';
    }
  }, [location.pathname]);

  return null;
};

const App: React.FC = () => {
  const [showSplash, setShowSplash] = useState(true);
  const [showOnboarding, setShowOnboarding] = useState(true);

  useEffect(() => {
    const timer = setTimeout(() => setShowSplash(false), 2500);
    return () => clearTimeout(timer);
  }, []);

  if (showSplash) return <SplashScreen />;
  const isAdminPath = typeof window !== 'undefined' && window.location.pathname.startsWith('/admin');
  if (showOnboarding && !isAdminPath) {
    return <Onboarding onFinish={() => setShowOnboarding(false)} />;
  }

  return (
    <ConfigProvider
      theme={{
        token: {
          colorPrimary: '#674fa3',
          fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
        },
      }}
    >
      <BrowserRouter>
        <TitleUpdater />
        <Routes>
          {/* Public Auth Routes (no MainLayout) */}
          <Route element={<RedirectIfAuthed />}>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
          </Route>

          {/* Admin Portal Routes */}
          <Route path="/admin/login" element={<AdminLogin />} />
          <Route
            path="/admin/*"
            element={
              <ProtectedRoute>
                <div className="app">
                  <AdminSidebar />
                  <div className="main-content">
                    <Routes>
                      <Route index element={<AdminDashboard />} />
                      <Route path="users" element={<AdminUserList />} />
                      <Route path="emission-factors" element={<AdminEmissionFactors />} />
                      <Route path="community-analytics" element={<AdminCommunityAnalytics />} />
                      <Route path="settings" element={<AdminSettings />} />
                    </Routes>
                  </div>
                </div>
              </ProtectedRoute>
            }
          />

          {/* User Portal Routes (requires login) */}
          <Route element={<RequireUserAuth />}>
            <Route element={<MainLayout />}>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<Dashboard />} />
              <Route path="/ai-assistant" element={<AIAssistant />} />
              <Route path="/leaderboard" element={<Leaderboard />} />
              <Route path="/profile" element={<Profile />} />
              <Route path="/about-me" element={<AboutMe />} />
              <Route path="/records" element={<Records />} />
              <Route path="/log-meal" element={<LogMeal />} />
              <Route path="/log-travel" element={<LogTravel />} />
              <Route path="/log-utility" element={<LogUtility />} />
              <Route path="/tree-planting" element={<TreePlanting />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </ConfigProvider>
  );
};

export default App;
