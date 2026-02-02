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
  const [password, setPassword] = useState<string>('');
  const [avatarUrl, setAvatarUrl] = useState<string>('');
  const watchedPassword: string = Form.useWatch('password', form) ?? '';

  // 🔍 调试：监听 avatarUrl 和 user.avatar 的变化
  useEffect(() => {
    console.log('=== Avatar State Change Debug ===');
    console.log('avatarUrl:', avatarUrl?.substring(0, 50));
    console.log('user.avatar:', user.avatar?.substring(0, 50));
    console.log('Final src (avatarUrl || user.avatar):', (avatarUrl || user.avatar)?.substring(0, 50));
  }, [avatarUrl, user.avatar]);

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
          // 如果是 Base64 字符串（data:image 开头），直接返回
          if (url.startsWith('data:image')) return url;
          // 如果是完整 URL（http/https 开头），直接返回
          if (url.startsWith('http')) return url;
          // 否则拼接基础 URL（兼容旧格式）
          return `${baseUrl}${url}`;
        };

        if (me) {
          const avatar = normalizeUrl(me.avatar);
          console.log('=== Initial Load Avatar Debug ===');
          console.log('1. me.avatar (raw):', me.avatar?.substring(0, 50));
          console.log('2. Normalized avatar:', avatar?.substring(0, 50));
          console.log('3. Avatar length:', avatar?.length);
          console.log('4. Is Base64?', avatar?.startsWith('data:image'));
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

  const passwordStrength = useMemo(() => {
    const pwd = watchedPassword ?? '';
    const hasLower = /[a-z]/.test(pwd);
    const hasUpper = /[A-Z]/.test(pwd);
    const hasDigit = /\d/.test(pwd);
    const hasLetter = hasLower || hasUpper;
    const isStrong = pwd.length >= 9 && hasLower && hasUpper && hasDigit;
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
      password: password || undefined,
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

    const pwd = values.password as string | undefined;
    if (pwd && pwd.trim() !== '') {
      const hasLower = /[a-z]/.test(pwd);
      const hasUpper = /[A-Z]/.test(pwd);
      const hasDigit = /\d/.test(pwd);
      const hasLetter = hasLower || hasUpper;
      const mediumOrAbove = pwd.length >= 9 && hasLetter && hasDigit;
      if (!mediumOrAbove) {
        message.error('Password is too weak (at least 9 characters, including letters and numbers).');
        return;
      }
    }

    try {
      const payload: { nickname?: string; email?: string; location?: string; birthDate?: string; password?: string } = {
        nickname: values.nickname,
        email: values.email,
        location: values.location,
        birthDate: values.birthDate ? values.birthDate.format('YYYY-MM-DD') : undefined,
      };
      if (pwd && pwd.trim() !== '') payload.password = pwd;

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
      if (pwd) setPassword(pwd);

      setIsEditing(false);
      message.success('Profile updated successfully');
    } catch (e: any) {
      const msg = e.response?.data?.message ?? e.response?.data ?? e.message ?? 'Update failed';
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

      // 🔍 调试日志：查看原始响应
      console.log('=== Avatar Upload Response Debug ===');
      console.log('1. Raw response:', response);
      console.log('2. Response type:', typeof response);
      console.log('3. Response keys:', Object.keys(response || {}));
      console.log('4. response.avatarUrl:', response?.avatarUrl);
      console.log('5. response.avatar:', response?.avatar);

      const newAvatarPath = response?.avatarUrl || response?.avatar;
      console.log('6. Extracted newAvatarPath:', newAvatarPath);
      console.log('7. newAvatarPath type:', typeof newAvatarPath);
      console.log('8. newAvatarPath length:', newAvatarPath?.length);
      console.log('9. Starts with data:image?', newAvatarPath?.startsWith('data:image'));
      console.log('10. Starts with http?', newAvatarPath?.startsWith('http'));

      if (newAvatarPath) {
        // 如果是 Base64 字符串（data:image 开头），直接使用
        // 如果是完整 URL（http/https 开头），直接使用
        // 否则拼接基础 URL（兼容旧格式）
        const fullUrl = newAvatarPath.startsWith('data:image') || newAvatarPath.startsWith('http')
          ? newAvatarPath 
          : `${import.meta.env.VITE_API_URL || ''}${newAvatarPath}`;
        
        console.log('11. Final fullUrl:', fullUrl?.substring(0, 100) + '...'); // 只显示前100个字符
        console.log('12. fullUrl length:', fullUrl?.length);
        console.log('13. VITE_API_URL:', import.meta.env.VITE_API_URL);
        
        setAvatarUrl(fullUrl);
        setUser((prev) => {
          console.log('14. Previous user.avatar:', prev.avatar?.substring(0, 50));
          const updated = { ...prev, avatar: fullUrl };
          console.log('15. Updated user.avatar:', updated.avatar?.substring(0, 50));
          return updated;
        });
        
        // 验证状态是否更新
        setTimeout(() => {
          console.log('16. Current avatarUrl state:', avatarUrl?.substring(0, 50));
          console.log('17. Current user.avatar state:', user.avatar?.substring(0, 50));
        }, 100);
        
        message.success({ content: 'Avatar uploaded and saved!', key: 'avatarUpload' });
      } else {
        console.error('❌ No avatar path found in response!');
        console.error('Response structure:', JSON.stringify(response, null, 2));
      }
    } catch (e: any) {
      console.error('❌ Avatar upload failed:', e);
      console.error('Error response:', e.response);
      console.error('Error data:', e.response?.data);
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
                      onError={(e) => {
                        console.error('❌ Avatar image load error:', e);
                        console.error('Failed src:', avatarUrl || user.avatar);
                        console.error('src length:', (avatarUrl || user.avatar)?.length);
                        console.error('src starts with data:image?', (avatarUrl || user.avatar)?.startsWith('data:image'));
                      }}
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
                <Form.Item label="Password" name="password">
                  <Input.Password placeholder="Enter new password if you want to change it" />
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
