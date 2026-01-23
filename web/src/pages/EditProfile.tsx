import { useState } from 'react';
import { Card, Form, Input, DatePicker, Select, Button, message, Row, Col } from 'antd';
import { useNavigate } from 'react-router-dom';
import { SaveOutlined, CloseOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { LocationEnum } from '../types/index';
import './EditProfile.module.css';

const EditProfile = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const locationOptions: { label: string; value: LocationEnum }[] = [
    { label: 'West Region', value: 'West Region' },
    { label: 'North Region', value: 'North Region' },
    { label: 'North-East Region', value: 'North-East Region' },
    { label: 'East Region', value: 'East Region' },
    { label: 'Central Region', value: 'Central Region' },
  ];

  // Mock initial data
  const initialValues = {
    email: 'melody@example.com',
    password: '',
    birthDate: dayjs('1995-03-15'),
    location: 'West Region' as LocationEnum,
  };

  const handleSave = async () => {
    try {
      setLoading(true);
      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 500));
      
      message.success('Saved successfully');
      
      // Redirect to profile
      navigate('/profile');
    } catch (error) {
      message.error('Failed to save changes');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    navigate('/profile');
  };

  return (
    <div style={{ width: '100%', minHeight: 'calc(100vh - 200px)', display: 'flex', alignItems: 'flex-start', justifyContent: 'center' }}>
      <Row gutter={[24, 24]} justify="center">
        <Col xs={24} sm={24} md={16} lg={12}>
          <Card style={{ borderRadius: '12px', boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)', marginTop: '24px' }}>
            <div style={{ marginBottom: '24px' }}>
              <div style={{ fontSize: '22px', fontWeight: '700', color: '#333', marginBottom: '4px' }}>Edit Personal Information</div>
              <div style={{ fontSize: '14px', color: '#666' }}>Update your profile details</div>
            </div>

            <Form
              form={form}
              layout="vertical"
              initialValues={initialValues}
              onFinish={handleSave}
              autoComplete="off"
            >
              <Form.Item
                label="Email"
                name="email"
                rules={[
                  { required: true, message: 'Please enter your email' },
                  { type: 'email', message: 'Please enter a valid email' },
                ]}
              >
                <Input placeholder="Enter your email" size="large" />
              </Form.Item>

              <Form.Item
                label="Password"
                name="password"
                rules={[
                  { min: 6, message: 'Password must be at least 6 characters' },
                ]}
              >
                <Input.Password placeholder="Leave blank to keep current password" size="large" />
              </Form.Item>

              <Form.Item
                label="Birth Date"
                name="birthDate"
                rules={[{ required: true, message: 'Please select your birth date' }]}
              >
                <DatePicker
                  style={{ width: '100%' }}
                  size="large"
                  format="YYYY-MM-DD"
                />
              </Form.Item>

              <Form.Item
                label="Location"
                name="location"
                rules={[{ required: true, message: 'Please select your location' }]}
              >
                <Select
                  placeholder="Select your location"
                  options={locationOptions}
                  size="large"
                />
              </Form.Item>

              <Form.Item style={{ marginTop: '32px', marginBottom: 0 }}>
                <Row gutter={12}>
                  <Col xs={12}>
                    <Button
                      type="primary"
                      icon={<SaveOutlined />}
                      htmlType="submit"
                      block
                      size="large"
                      loading={loading}
                      style={{
                        background: '#674fa3',
                        borderColor: '#674fa3',
                      }}
                    >
                      Save
                    </Button>
                  </Col>
                  <Col xs={12}>
                    <Button
                      icon={<CloseOutlined />}
                      onClick={handleCancel}
                      block
                      size="large"
                    >
                      Cancel
                    </Button>
                  </Col>
                </Row>
              </Form.Item>
            </Form>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default EditProfile;
