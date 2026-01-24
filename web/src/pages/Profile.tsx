import { useMemo, useState } from 'react';
import { Card, Descriptions, Avatar, Button, Row, Col, Form, Input, Select, DatePicker, message, Space, Progress, Upload } from 'antd';
import type { UploadProps } from 'antd';
import dayjs from 'dayjs';
import { EditOutlined, MailOutlined, EnvironmentOutlined, CalendarOutlined, SaveOutlined, CloseOutlined, LockOutlined, UserOutlined, CameraOutlined } from '@ant-design/icons';
import { User } from '../types/index';
import { updateLeaderboardAvatar, updateLeaderboardNickname } from '../mock/data';
import styles from './Profile.module.css';

const Profile = () => {
  const [form] = Form.useForm();
  const [isEditing, setIsEditing] = useState(false);

  // Mock data
  const [user, setUser] = useState<User>({
    id: '1',
    name: 'Melody',
    nickname: 'EcoRanger',
    email: 'melody@example.com',
    location: 'West Region',
    birthDate: '1995-03-15',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Melody',
    joinDays: 127,
  });

  const [password, setPassword] = useState<string>('password123');
  const [avatarUrl, setAvatarUrl] = useState<string>(user.avatar);
  const watchedPassword: string = Form.useWatch('password', form) ?? '';

  const joinDateText = useMemo(() => {
    return dayjs().subtract(user.joinDays, 'day').format('MMMM DD, YYYY');
  }, [user.joinDays]);

  const passwordStrength = useMemo(() => {
    const pwd = watchedPassword ?? '';
    const hasLower = /[a-z]/.test(pwd);
    const hasUpper = /[A-Z]/.test(pwd);
    const hasDigit = /\d/.test(pwd);
    const hasLetter = hasLower || hasUpper;

    // New requirement:
    // Strong:
    // - length > 8 (>= 9)
    // - contains uppercase + lowercase + digit
    const isStrong = pwd.length >= 9 && hasLower && hasUpper && hasDigit;

    // Medium:
    // - length > 8 (>= 9)
    // - contains letters + digits (case-insensitive)
    const isMedium = pwd.length >= 9 && hasLetter && hasDigit;

    if (isStrong) return { label: 'Strong', percent: 100, color: '#52c41a', canSave: true };
    if (isMedium) return { label: 'Medium', percent: 66, color: '#faad14', canSave: true };
    if (pwd.length === 0) return { label: 'Weak', percent: 0, color: '#ff4d4f', canSave: false };
    return { label: 'Weak', percent: 33, color: '#ff4d4f', canSave: false };
  }, [watchedPassword]);

  const locationOptions = useMemo(
    () => [
      { label: 'West Region', value: 'West Region' },
      { label: 'North Region', value: 'North Region' },
      { label: 'North-East Region', value: 'North-East Region' },
      { label: 'East Region', value: 'East Region' },
      { label: 'Central Region', value: 'Central Region' },
    ],
    []
  );

  const startEditing = () => {
    setIsEditing(true);
    form.setFieldsValue({
      username: user.name,
      nickname: user.nickname,
      email: user.email,
      password,
      location: user.location,
      birthDate: dayjs(user.birthDate),
    });
  };

  const cancelEditing = () => {
    setIsEditing(false);
    form.resetFields();
  };

  const saveProfile = async () => {
    const values = await form.validateFields();

    const pwd = values.password as string;
    const hasLower = /[a-z]/.test(pwd);
    const hasUpper = /[A-Z]/.test(pwd);
    const hasDigit = /\d/.test(pwd);
    const hasLetter = hasLower || hasUpper;
    const mediumOrAbove = pwd.length >= 9 && hasLetter && hasDigit;
    if (!mediumOrAbove) {
      message.error('密码强度不足');
      return;
    }

    setUser((prev) => ({
      ...prev,
      nickname: values.nickname,
      email: values.email,
      location: values.location,
      birthDate: values.birthDate.format('YYYY-MM-DD'),
    }));
    updateLeaderboardNickname(user.name, values.nickname);
    setPassword(values.password);
    setIsEditing(false);
    message.success('Profile updated successfully');
  };

  const fileToBase64 = (file: File) =>
    new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });

  const beforeUpload: UploadProps['beforeUpload'] = async (file) => {
    const base64 = await fileToBase64(file as unknown as File);
    setAvatarUrl(base64);
    setUser((prev) => ({ ...prev, avatar: base64 }));
    updateLeaderboardAvatar(user.name, base64);
    message.success('Avatar updated');
    return false;
  };

  return (
    <div style={{ width: '100%' }}>
      <Row gutter={[24, 24]}>
        {/* User Info Card */}
        <Col xs={24} md={8}>
          <Card style={{ borderRadius: '12px', boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
            <div style={{ textAlign: 'center' }}>
              <Upload accept="image/*" showUploadList={false} beforeUpload={beforeUpload}>
                <div className={styles.avatarUploadWrapper} aria-label="Upload avatar">
                  <Avatar
                    src={avatarUrl}
                    size={120}
                    style={{ border: '3px solid #674fa3' }}
                  />
                  <div className={styles.avatarOverlay}>
                    <CameraOutlined />
                    <span style={{ marginLeft: 8, fontWeight: 600 }}>Edit</span>
                  </div>
                </div>
              </Upload>
            </div>
            <div style={{ textAlign: 'center', marginTop: '16px' }}>
              <div style={{ fontSize: '20px', fontWeight: '700', color: '#333', marginTop: '12px' }}>{user.name}</div>
              <div style={{ fontSize: '14px', color: '#666', marginTop: '4px' }}>{user.email}</div>
            </div>
          </Card>
        </Col>

        {/* Detailed Info */}
        <Col xs={24} md={16}>
          <Card style={{ borderRadius: '12px', boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
            <div style={{ marginBottom: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div style={{ fontSize: '18px', fontWeight: '600' }}>Personal Information</div>
              {!isEditing ? (
                <Button
                  type="primary"
                  icon={<EditOutlined />}
                  onClick={startEditing}
                  style={{ background: '#674fa3', borderColor: '#674fa3' }}
                >
                  Edit Profile
                </Button>
              ) : (
                <Space>
                  <Button
                    type="primary"
                    icon={<SaveOutlined />}
                    onClick={saveProfile}
                    style={{ background: '#674fa3', borderColor: '#674fa3' }}
                  >
                    Save
                  </Button>
                  <Button icon={<CloseOutlined />} onClick={cancelEditing}>
                    Cancel
                  </Button>
                </Space>
              )}
            </div>

            {!isEditing ? (
              <Descriptions column={1} size="middle" style={{ marginTop: '24px' }}>
                <Descriptions.Item
                  label={
                    <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <UserOutlined /> Username
                    </span>
                  }
                >
                  {user.name}
                </Descriptions.Item>
                <Descriptions.Item label="Nickname">{user.nickname}</Descriptions.Item>
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
                      <LockOutlined /> Password
                    </span>
                  }
                >
                  ********
                </Descriptions.Item>
                <Descriptions.Item
                  label={
                    <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <CalendarOutlined /> Birth Date
                    </span>
                  }
                >
                  {dayjs(user.birthDate).format('MMMM DD, YYYY')}
                </Descriptions.Item>
                <Descriptions.Item
                  label={
                    <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <EnvironmentOutlined /> Location
                    </span>
                  }
                >
                  {user.location}
                </Descriptions.Item>
                <Descriptions.Item label="Join Date">{joinDateText}</Descriptions.Item>
              </Descriptions>
            ) : (
              <Form
                form={form}
                layout="vertical"
                autoComplete="off"
                style={{ marginTop: '24px' }}
              >
                <Form.Item label="Username" name="username">
                  <Input disabled />
                </Form.Item>

                <Form.Item
                  label="Nickname"
                  name="nickname"
                  rules={[{ required: true, message: 'Please enter your nickname' }]}
                >
                  <Input placeholder="Enter your nickname" />
                </Form.Item>

                <Form.Item
                  label="Email"
                  name="email"
                  rules={[
                    { required: true, message: 'Please enter your email' },
                    { type: 'email', message: 'Please enter a valid email' },
                  ]}
                >
                  <Input placeholder="Enter your email" />
                </Form.Item>

                <Form.Item
                  label="Password"
                  name="password"
                  rules={[{ required: true, message: 'Please enter your password' }]}
                >
                  <Input.Password placeholder="Enter your password" />
                </Form.Item>

                <div style={{ marginTop: -8, marginBottom: 16 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, color: '#666', marginBottom: 6 }}>
                    <span>Password strength</span>
                    <span style={{ color: passwordStrength.color, fontWeight: 600 }}>{passwordStrength.label}</span>
                  </div>
                  <Progress
                    percent={passwordStrength.percent}
                    showInfo={false}
                    strokeColor={passwordStrength.color}
                  />
                </div>

                <Form.Item
                  label="Location"
                  name="location"
                  rules={[{ required: true, message: 'Please select your location' }]}
                >
                  <Select options={locationOptions} />
                </Form.Item>

                <Form.Item
                  label="Birth Date"
                  name="birthDate"
                  rules={[{ required: true, message: 'Please select your birth date' }]}
                >
                  <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
                </Form.Item>
              </Form>
            )}
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Profile;
