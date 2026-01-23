import { Layout, Menu, Button, Avatar, Space } from 'antd';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import dayjs from 'dayjs';
import {
  DashboardOutlined,
  FileTextOutlined,
  UserOutlined,
  BulbOutlined,
} from '@ant-design/icons';
import './MainLayout.module.css';

const { Sider, Header, Content } = Layout;

const MainLayout: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const getSelectedKey = () => {
    if (location.pathname.startsWith('/profile')) return 'profile';
    if (location.pathname.startsWith('/records')) return 'records';
    return 'dashboard';
  };

  const menuItems = [
    {
      key: 'dashboard',
      icon: <DashboardOutlined />,
      label: 'Dashboard',
      onClick: () => navigate('/'),
    },
    {
      key: 'records',
      icon: <FileTextOutlined />,
      label: 'Records',
      onClick: () => navigate('/records'),
    },
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Profile',
      onClick: () => navigate('/profile'),
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* Sidebar */}
      <Sider width={240} style={{ background: '#fff' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', padding: '20px', borderBottom: '1px solid #f0f0f0', marginBottom: '16px' }}>
          <img
            src="/src/assets/icons/splash.svg"
            alt="Logo"
            style={{ width: '40px', height: '40px' }}
          />
          <span style={{ fontSize: '14px', fontWeight: '600', color: '#674fa3', whiteSpace: 'nowrap' }}>My Carbon Ledger</span>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[getSelectedKey()]}
          items={menuItems}
          style={{ borderRight: 'none' }}
        />
      </Sider>

      {/* Main Content */}
      <Layout>
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
            <Button type="default" icon={<BulbOutlined />}>
              Tips
            </Button>
            <Avatar icon={<UserOutlined />} style={{ background: '#674fa3' }} />
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
