import { Card, Descriptions, Avatar, Button, Tag, Row, Col } from 'antd';
import { useNavigate } from 'react-router-dom';
import { EditOutlined, MailOutlined, EnvironmentOutlined, CalendarOutlined } from '@ant-design/icons';
import { User } from '../types/index';
import './Profile.module.css';

const Profile = () => {
  const navigate = useNavigate();

  // Mock data
  const user: User = {
    id: '1',
    name: 'Melody',
    email: 'melody@example.com',
    location: 'West Region',
    birthDate: '1995-03-15',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Melody',
    joinDays: 127,
  };

  return (
    <div style={{ width: '100%' }}>
      <Row gutter={[24, 24]}>
        {/* User Info Card */}
        <Col xs={24} md={8}>
          <Card style={{ borderRadius: '12px', boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
            <div style={{ textAlign: 'center' }}>
              <Avatar
                src={user.avatar}
                size={120}
                style={{ border: '3px solid #674fa3' }}
              />
            </div>
            <div style={{ textAlign: 'center', marginTop: '16px' }}>
              <div style={{ fontSize: '20px', fontWeight: '700', color: '#333', marginTop: '12px' }}>{user.name}</div>
              <div style={{ fontSize: '14px', color: '#666', marginTop: '4px' }}>{user.email}</div>
              <Tag color="purple" style={{ marginTop: '12px' }}>
                Member for {user.joinDays} days
              </Tag>
            </div>
          </Card>
        </Col>

        {/* Detailed Info */}
        <Col xs={24} md={16}>
          <Card style={{ borderRadius: '12px', boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
            <div style={{ marginBottom: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div style={{ fontSize: '18px', fontWeight: '600' }}>Personal Information</div>
              <Button
                type="primary"
                icon={<EditOutlined />}
                onClick={() => navigate('/profile/edit')}
                style={{
                  background: '#674fa3',
                  borderColor: '#674fa3',
                }}
              >
                Edit Profile
              </Button>
            </div>

            <Descriptions
              column={1}
              size="middle"
              style={{ marginTop: '24px' }}
            >
              <Descriptions.Item
                label={
                  <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <MailOutlined /> Email
                  </span>
                }
              >
                {user.email}
              </Descriptions.Item>
              <Descriptions.Item
                label={
                  <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <EnvironmentOutlined /> Location
                  </span>
                }
              >
                <Tag color="purple">{user.location}</Tag>
              </Descriptions.Item>
              <Descriptions.Item
                label={
                  <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <CalendarOutlined /> Birth Date
                  </span>
                }
              >
                {new Date(user.birthDate).toLocaleDateString('en-US', {
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                })}
              </Descriptions.Item>
              <Descriptions.Item label="Member Since">
                {user.joinDays} days ago
              </Descriptions.Item>
            </Descriptions>
          </Card>

          {/* Stats Cards */}
          <Row gutter={16} style={{ marginTop: '24px' }}>
            <Col xs={12} sm={8}>
              <Card style={{ borderRadius: '8px', border: '1px solid #f0f0f0', textAlign: 'center', transition: 'all 0.3s ease' }}>
                <div style={{ fontSize: '24px', fontWeight: '700', color: '#674fa3' }}>127</div>
                <div style={{ fontSize: '12px', color: '#666', marginTop: '8px' }}>Days</div>
              </Card>
            </Col>
            <Col xs={12} sm={8}>
              <Card style={{ borderRadius: '8px', border: '1px solid #f0f0f0', textAlign: 'center', transition: 'all 0.3s ease' }}>
                <div style={{ fontSize: '24px', fontWeight: '700', color: '#674fa3' }}>5.2</div>
                <div style={{ fontSize: '12px', color: '#666', marginTop: '8px' }}>kg CO₂e</div>
              </Card>
            </Col>
            <Col xs={12} sm={8}>
              <Card style={{ borderRadius: '8px', border: '1px solid #f0f0f0', textAlign: 'center', transition: 'all 0.3s ease' }}>
                <div style={{ fontSize: '24px', fontWeight: '700', color: '#674fa3' }}>18</div>
                <div style={{ fontSize: '12px', color: '#666', marginTop: '8px' }}>Logs</div>
              </Card>
            </Col>
          </Row>
        </Col>
      </Row>
    </div>
  );
};

export default Profile;
