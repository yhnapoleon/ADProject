import { Layout, Menu, Button, Avatar, Space } from 'antd';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import dayjs from 'dayjs';
import logo from '../assets/icons/splash.svg';
import {
  AppstoreOutlined,
  CoffeeOutlined,
  CarOutlined,
  BulbOutlined,
  FileTextOutlined,
  UserOutlined,
  TrophyOutlined,
  RobotOutlined,
  ReadOutlined,
} from '@ant-design/icons';
import './MainLayout.module.css';

const { Sider, Header, Content } = Layout;

const MainLayout: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const isLoggingPage = location.pathname.startsWith('/log-');

  const getSelectedKey = () => {
    if (isLoggingPage) {
      if (location.pathname.startsWith('/log-meal')) return 'log-meal';
      if (location.pathname.startsWith('/log-travel')) return 'log-travel';
      if (location.pathname.startsWith('/log-utility')) return 'log-utility';
    }

    if (location.pathname.startsWith('/dashboard')) return 'dashboard';
    if (location.pathname.startsWith('/records')) return 'records';
    if (location.pathname.startsWith('/leaderboard')) return 'leaderboard';
    if (location.pathname.startsWith('/profile')) return 'profile';
    if (location.pathname.startsWith('/about-me')) return 'about-me';
    if (location.pathname.startsWith('/ai-assistant')) return 'ai-assistant';
    return 'dashboard';
  };

  const pathByKey: Record<string, string> = {
    dashboard: '/dashboard',
    records: '/records',
    leaderboard: '/leaderboard',
    profile: '/profile',
    'about-me': '/about-me',
    'ai-assistant': '/ai-assistant',
    'log-meal': '/log-meal',
    'log-travel': '/log-travel',
    'log-utility': '/log-utility',
  };

  const handleMenuClick = ({ key }: { key: string }) => {
    const path = pathByKey[key];
    if (path) navigate(path);
  };

  // Group A (default/daily mode)
  const groupAItems = [
    { key: 'dashboard', icon: <AppstoreOutlined />, label: 'Dashboard' },
    { key: 'records', icon: <FileTextOutlined />, label: 'Records' },
    { key: 'leaderboard', icon: <TrophyOutlined />, label: 'Leaderboard' },
    { key: 'profile', icon: <UserOutlined />, label: 'Profile' },
    { key: 'about-me', icon: <ReadOutlined />, label: 'About Me' },
    { key: 'ai-assistant', icon: <RobotOutlined />, label: 'AI Assistant' },
  ];

  // Group B (logging mode)
  const groupBItems = [
    { key: 'dashboard', icon: <AppstoreOutlined />, label: 'Dashboard' },
    { key: 'log-meal', icon: <CoffeeOutlined />, label: 'Food' },
    { key: 'log-travel', icon: <CarOutlined />, label: 'Travel' },
    { key: 'log-utility', icon: <BulbOutlined />, label: 'Utility' },
  ];

  const menuItems = isLoggingPage ? groupBItems : groupAItems;

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* Sidebar */}
      <Sider
        width={240}
        style={{
          background: '#fff',
          position: 'sticky',
          top: 0,
          height: '100vh',
          overflow: 'auto',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', padding: '20px', borderBottom: '1px solid #f0f0f0', marginBottom: '16px' }}>
          <img
            src={logo}
            alt="Logo"
            style={{ width: '40px', height: '40px' }}
          />
          <span style={{ fontSize: '14px', fontWeight: '600', color: '#674fa3', whiteSpace: 'nowrap' }}>My Carbon Ledger</span>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[getSelectedKey()]}
          items={menuItems}
          onClick={handleMenuClick}
          style={{ borderRight: 'none' }}
        />
      </Sider>

      {/* Main Content */}
      <Layout style={{ height: '100vh', overflow: 'auto' }}>
        {/* Header */}
        <Header
          style={{
            background: '#fff',
            padding: '0 24px',
            boxShadow: '0 1px 2px rgba(0,0,0,0.05)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            position: 'sticky',
            top: 0,
            zIndex: 10,
          }}
        >
          <div style={{ fontSize: '16px', color: '#666' }}>
            {dayjs().format('MMMM DD, YYYY')}
          </div>
          <Space size="middle">
            <Button type="default" icon={<RobotOutlined />} onClick={() => navigate('/ai-assistant')}>
              Ask AI
            </Button>
            <Button
              type="text"
              onClick={() => navigate('/profile')}
              aria-label="Go to profile"
              style={{ padding: 0, height: 'auto' }}
            >
              <Avatar icon={<UserOutlined />} style={{ background: '#674fa3' }} />
            </Button>
          </Space>
        </Header>

        {/* Content Area */}
        <Content style={{ margin: '24px', flex: 1 }}>
          <div style={{ background: '#fff', padding: '24px', borderRadius: '8px' }}>
            <Outlet />
          </div>
        </Content>
      </Layout>
    </Layout>
  );
};

export default MainLayout;
