import { useMemo, useState, useEffect } from 'react';
import { Card, Descriptions, Avatar, Button, Row, Col, Form, Input, Select, DatePicker, message, Space, Progress, Upload, Modal } from 'antd';
import type { UploadProps } from 'antd';
import dayjs from 'dayjs';
import { EditOutlined, MailOutlined, EnvironmentOutlined, CalendarOutlined, SaveOutlined, CloseOutlined, LockOutlined, UserOutlined, CameraOutlined, TrophyOutlined, LogoutOutlined } from '@ant-design/icons';
import { User } from '../types/index';
import styles from './Profile.module.css';
import { useNavigate } from 'react-router-dom';
import request from '../utils/request';

/** GET /api/user/me 返回结构 */
interface MeDto {
  id: string;
  name: string;
  nickname: string;
  email: string;
  location: string | null;
  birthDate: string | null;
  avatar: string | null;
  pointsWeek: number;
  pointsMonth: number;
  pointsTotal: number;
  joinDays: number;
}

/** GET /api/user/profile 返回结构（用于碳减排、排名） */
interface UserProfileApi {
  id: number;
  username: string;
  nickname: string;
  email: string;
  avatarUrl: string | null;
  region: string | null;
  totalCarbonSaved: number;
  currentPoints: number;
  rank: number;
  role: string;
}

const defaultUser: User = {
  id: '',
  name: '',
  nickname: '',
  email: '',
  location: 'West Region',
  birthDate: '',
  avatar: '',
  joinDays: 0,
  pointsWeek: 0,
  pointsMonth: 0,
  pointsTotal: 0,
};

const Profile = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [isEditing, setIsEditing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [user, setUser] = useState<User>(defaultUser);
  const [totalCarbonSaved, setTotalCarbonSaved] = useState(0);
  const [rank, setRank] = useState(0);
  const [avatarUrl, setAvatarUrl] = useState<string>('');
  const [passwordModalOpen, setPasswordModalOpen] = useState(false);
  const [passwordForm] = Form.useForm();
  const watchedNewPassword: string = Form.useWatch('newPassword', passwordForm) ?? '';

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const [meRes, profileRes] = await Promise.all([
          request.get('/api/user/me').catch(() => null),
          request.get('/api/user/profile').catch(() => null),
        ]);
        const me = meRes as MeDto | null;
        const profile = profileRes as UserProfileApi | null;

        const baseUrl = import.meta.env.VITE_API_URL || '';
        const normalizeUrl = (url: string | null) => {
          if (!url) return '';
          if (url.startsWith('http')) return url;
          return `${baseUrl}${url}`;
        };

        if (me) {
          const avatar = normalizeUrl(me.avatar);
          setUser({
            id: me.id,
            name: me.name,
            nickname: me.nickname,
            email: me.email,
            location: (me.location as User['location']) || 'West Region',
            birthDate: me.birthDate || '',
            avatar: avatar,
            joinDays: me.joinDays ?? 0,
            pointsWeek: me.pointsWeek ?? 0,
            pointsMonth: me.pointsMonth ?? 0,
            pointsTotal: me.pointsTotal ?? 0,
          });
          setAvatarUrl(avatar);
        }
        if (profile) {
          setTotalCarbonSaved(Number(profile.totalCarbonSaved ?? 0));
          setRank(profile.rank ?? 0);
          if (!me) {
            const avatar = normalizeUrl(profile.avatarUrl);
            setUser((prev) => ({
              ...prev,
              id: String(profile.id),
              name: profile.username,
              nickname: profile.nickname,
              email: profile.email,
              location: (profile.region as User['location']) || prev.location,
              avatar: avatar,
              pointsWeek: profile.currentPoints ?? prev.pointsWeek,
              pointsMonth: profile.currentPoints ?? prev.pointsMonth,
              pointsTotal: profile.currentPoints ?? prev.pointsTotal,
            }));
            setAvatarUrl(avatar);
          }
        }
        if (!me && !profile) {
          if ((meRes as any)?.response?.status === 401 || (profileRes as any)?.response?.status === 401) {
            navigate('/login', { replace: true });
            return;
          }
          message.error('Failed to load profile');
        }
      } catch (e: any) {
        if (e.response?.status === 401) {
          navigate('/login', { replace: true });
          return;
        }
        message.error(e.response?.data?.message || e.message || 'Failed to load profile');
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [navigate]);

  const joinDateText = useMemo(() => {
    if (user.joinDays === undefined || user.joinDays === null) return '—';
    return `${user.joinDays} days`;
  }, [user.joinDays]);

  // Password strength logic aligned with Register page
  const passwordStrength = useMemo(() => {
    const pwd = watchedNewPassword ?? '';
    const hasLower = /[a-z]/.test(pwd);
    const hasUpper = /[A-Z]/.test(pwd);
    const hasDigit = /\d/.test(pwd);
    const hasLetter = hasLower || hasUpper;

    if (pwd.length < 8) {
      if (pwd.length === 0) return { label: 'Weak', percent: 0, color: '#ff4d4f', canSave: false };
      return { label: 'Weak', percent: 33, color: '#ff4d4f', canSave: false };
    }

    const isStrong = hasLower && hasUpper && hasDigit;
    const isMedium = hasLetter && hasDigit;

    if (isStrong) return { label: 'Strong', percent: 100, color: '#52c41a', canSave: true };
    if (isMedium) return { label: 'Medium', percent: 66, color: '#faad14', canSave: true };
    return { label: 'Weak', percent: 33, color: '#ff4d4f', canSave: false };
  }, [watchedNewPassword]);

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
      location: user.location,
      birthDate: user.birthDate ? dayjs(user.birthDate) : undefined,
    });
  };

  const cancelEditing = () => {
    setIsEditing(false);
    form.resetFields();
  };

  const saveProfile = async () => {
    const values = await form.validateFields();
    try {
      const payload: { nickname?: string; email?: string; location?: string; birthDate?: string } = {
        nickname: values.nickname,
        email: values.email,
        location: values.location,
        birthDate: values.birthDate ? values.birthDate.format('YYYY-MM-DD') : undefined,
      };
      const meRes: MeDto = await request.put('/api/user/me', payload);
      setUser((prev) => ({
        ...prev,
        name: meRes.name,
        nickname: meRes.nickname,
        email: meRes.email,
        location: (meRes.location as User['location']) || prev.location,
        birthDate: meRes.birthDate || prev.birthDate,
        avatar: meRes.avatar || prev.avatar,
        pointsWeek: meRes.pointsWeek ?? prev.pointsWeek,
        pointsMonth: meRes.pointsMonth ?? prev.pointsMonth,
        pointsTotal: meRes.pointsTotal ?? prev.pointsTotal,
        joinDays: meRes.joinDays ?? prev.joinDays,
      }));
      setIsEditing(false);
      message.success('Profile updated successfully');
    } catch (e: any) {
      const msg = e.response?.data?.message ?? e.response?.data ?? e.message ?? 'Update failed';
      message.error(typeof msg === 'string' ? msg : msg.message || msg);
    }
  };

  const submitChangePassword = async () => {
    try {
      const values = await passwordForm.validateFields();
      if (!passwordStrength.canSave) {
        message.error('Password must be at least medium strength (length >= 8, contains letters and digits).');
        return;
      }
      await request.post('/api/user/change-password', {
        oldPassword: values.oldPassword,
        newPassword: values.newPassword,
      });
      message.success('Password changed successfully');
      setPasswordModalOpen(false);
      passwordForm.resetFields();
    } catch (e: any) {
      if (e.errorFields) throw e; // validation error: keep modal open, no toast
      const msg = e.response?.data ?? e.message ?? 'Failed to change password';
      message.error(typeof msg === 'string' ? msg : msg.message || msg);
    }
  };

  const fileToBase64 = (file: File) =>
    new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });

  const beforeUpload: UploadProps['beforeUpload'] = async (file) => {
    const formData = new FormData();
    formData.append('file', file as any);

    try {
      message.loading({ content: 'Uploading avatar...', key: 'avatarUpload' });
      // 移除手动设置的 Content-Type，让浏览器和 axios 自动处理 boundary
      const response: any = await request.put('/api/user/avatar', formData);

      const newAvatarPath = response.avatarUrl || response.avatar;
      if (newAvatarPath) {
        // 如果返回的是相对路径，补全基础 URL
        const fullUrl = newAvatarPath.startsWith('http') 
          ? newAvatarPath 
          : `${import.meta.env.VITE_API_URL || ''}${newAvatarPath}`;
        
        setAvatarUrl(fullUrl);
        setUser((prev) => ({ ...prev, avatar: fullUrl }));
        message.success({ content: 'Avatar uploaded and saved!', key: 'avatarUpload' });
      }
    } catch (e: any) {
      console.error('Avatar upload failed:', e);
      message.error({ content: 'Failed to upload avatar', key: 'avatarUpload' });
    }
    return false;
  };

  const handleLogout = () => {
    Modal.confirm({
      title: 'Are you sure you want to log out?',
      okText: 'Log Out',
      okButtonProps: { danger: true },
      cancelText: 'Cancel',
      onOk: () => {
        localStorage.removeItem('isLoggedIn');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/login', { replace: true });
      },
    });
  };

  if (loading) {
    return (
      <div style={{ width: '100%', padding: 24, textAlign: 'center' }}>
        <Card loading style={{ maxWidth: 400, margin: '0 auto' }} />
      </div>
    );
  }

  return (
    <div style={{ width: '100%' }}>
      <Row gutter={[24, 24]}>
        <Col xs={24} md={8}>
          <Card style={{ borderRadius: '12px', boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
            <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
              <div style={{ textAlign: 'center' }}>
                <Upload accept="image/*" showUploadList={false} beforeUpload={beforeUpload}>
                  <div className={styles.avatarUploadWrapper} aria-label="Upload avatar">
                    <Avatar
                      src={avatarUrl || user.avatar}
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
                <div style={{ marginTop: 14, display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 8, color: '#333' }}>
                  <TrophyOutlined style={{ color: '#FFD700' }} />
                  <span style={{ fontWeight: 600 }}>Total Points:</span>
                  <span style={{ fontWeight: 700 }}>{user.pointsTotal}</span>
                </div>
                {rank > 0 && (
                  <div style={{ marginTop: 4, fontSize: 14, color: '#666' }}>
                    Rank: <strong>#{rank}</strong>
                  </div>
                )}
              </div>
              <div style={{ flex: 1 }} />
              <Button
                block
                danger
                type="text"
                icon={<LogoutOutlined />}
                onClick={handleLogout}
                style={{ marginTop: 18, fontWeight: 700 }}
              >
                Log Out
              </Button>
            </div>
          </Card>
        </Col>

        <Col xs={24} md={16}>
          <Card style={{ borderRadius: '12px', boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
            <div style={{ marginBottom: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div style={{ fontSize: '18px', fontWeight: '600' }}>Personal Information</div>
              {!isEditing ? (
                <Space>
                  <Button
                    type="primary"
                    icon={<EditOutlined />}
                    onClick={startEditing}
                    style={{ background: '#674fa3', borderColor: '#674fa3' }}
                  >
                    Edit Profile
                  </Button>
                  <Button
                    icon={<LockOutlined />}
                    onClick={() => setPasswordModalOpen(true)}
                  >
                    Change Password
                  </Button>
                </Space>
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
                    {user.birthDate ? dayjs(user.birthDate).format('MMMM DD, YYYY') : '—'}
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
                  <Descriptions.Item label="Join Date">{user.joinDays} days</Descriptions.Item>
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

      <Modal
        title="Change Password"
        open={passwordModalOpen}
        onCancel={() => { setPasswordModalOpen(false); passwordForm.resetFields(); }}
        onOk={submitChangePassword}
        okText="Change password"
        cancelText="Cancel"
        destroyOnClose
      >
        <Form form={passwordForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            label="Current password"
            name="oldPassword"
            rules={[{ required: true, message: 'Please enter your current password' }]}
          >
            <Input.Password placeholder="Enter current password" autoComplete="current-password" />
          </Form.Item>
          <Form.Item
            label="New password"
            name="newPassword"
            rules={[
              { required: true, message: 'Please enter a new password' },
              { min: 8, message: 'At least 8 characters' },
            ]}
          >
            <Input.Password placeholder="Enter new password" autoComplete="new-password" />
          </Form.Item>
          <div style={{ marginTop: -8, marginBottom: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, color: '#666', marginBottom: 6 }}>
              <span>Password strength</span>
              <span style={{ color: passwordStrength.color, fontWeight: 600 }}>{passwordStrength.label}</span>
            </div>
            <Progress percent={passwordStrength.percent} showInfo={false} strokeColor={passwordStrength.color} />
          </div>
          <Form.Item
            label="Confirm new password"
            name="confirmPassword"
            dependencies={['newPassword']}
            rules={[
              { required: true, message: 'Please confirm your new password' },
              ({ getFieldValue }: { getFieldValue: (name: string) => unknown }) => ({
                validator(_: unknown, value: string) {
                  if (!value || getFieldValue('newPassword') === value) return Promise.resolve();
                  return Promise.reject(new Error('Passwords do not match'));
                },
              }),
            ]}
          >
            <Input.Password placeholder="Confirm new password" autoComplete="new-password" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Profile;
