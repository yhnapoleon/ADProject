import { Button, Card, Form, Input, message, Typography, DatePicker, Select, Progress } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import { useMemo } from 'react';
import splashIcon from '../assets/icons/splash.svg';

const { Title, Text } = Typography;

type FormValues = {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  dateOfBirth: any;
  location: string;
};

const Register = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm<FormValues>();
  const watchedPassword: string = Form.useWatch('password', form) ?? '';

  const passwordStrength = useMemo(() => {
    const pwd = watchedPassword ?? '';
    const hasLower = /[a-z]/.test(pwd);
    const hasUpper = /[A-Z]/.test(pwd);
    const hasDigit = /\d/.test(pwd);
    const hasLetter = hasLower || hasUpper;

    // Weak: length < 8
    if (pwd.length < 8) {
      if (pwd.length === 0) return { label: 'Weak', percent: 0, color: '#ff4d4f', canSave: false };
      return { label: 'Weak', percent: 33, color: '#ff4d4f', canSave: false };
    }

    // Strong: length >= 8 && contains uppercase + lowercase + digit
    const isStrong = hasLower && hasUpper && hasDigit;

    // Medium: length >= 8 && contains letters + digits (case-insensitive)
    const isMedium = hasLetter && hasDigit;

    if (isStrong) return { label: 'Strong', percent: 100, color: '#52c41a', canSave: true };
    if (isMedium) return { label: 'Medium', percent: 66, color: '#faad14', canSave: true };
    return { label: 'Weak', percent: 33, color: '#ff4d4f', canSave: false };
  }, [watchedPassword]);

  const locationOptions = [
    { label: 'West Region', value: 'West Region' },
    { label: 'North Region', value: 'North Region' },
    { label: 'North-East Region', value: 'North-East Region' },
    { label: 'East Region', value: 'East Region' },
    { label: 'Central Region', value: 'Central Region' },
  ];

  const onFinish = async (values: FormValues) => {
    // Check password strength - must be Medium or Strong
    if (!passwordStrength.canSave) {
      message.error('Password must be at least medium strength (length >= 8, contains letters and digits)');
      return;
    }
    message.success('Account created successfully!');
    navigate('/login', { replace: true });
  };

  return (
    <div
      style={{
        width: '100vw',
        height: '100vh',
        background: '#f5f5f5',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 16,
      }}
    >
      <Card style={{ width: 'min(460px, 92vw)', borderRadius: 12, boxShadow: '0 2px 10px rgba(0,0,0,0.06)' }}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginBottom: 18 }}>
          <img src={splashIcon} alt="EcoLens" style={{ width: 64, height: 64 }} />
          <div style={{ marginTop: 10, fontWeight: 800, color: '#674fa3', fontSize: 20 }}>EcoLens</div>
          <Title level={3} style={{ marginTop: 10, marginBottom: 0 }}>
            Create Account
          </Title>
        </div>

        <Form<FormValues> form={form} layout="vertical" onFinish={onFinish} autoComplete="off">
          <Form.Item
            label="Username"
            name="username"
            rules={[{ required: true, message: 'Please enter a username' }]}
          >
            <Input placeholder="Your username" />
          </Form.Item>

          <Form.Item
            label="Email"
            name="email"
            rules={[
              { required: true, message: 'Please enter your email' },
              { type: 'email', message: 'Please enter a valid email' },
            ]}
          >
            <Input placeholder="you@example.com" />
          </Form.Item>

          <Form.Item
            label="Password"
            name="password"
            rules={[{ required: true, message: 'Please enter a password' }]}
          >
            <Input.Password placeholder="Create password" />
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
            label="Confirm Password"
            name="confirmPassword"
            dependencies={['password']}
            rules={[
              { required: true, message: 'Please confirm your password' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('password') === value) return Promise.resolve();
                  return Promise.reject(new Error('Passwords do not match'));
                },
              }),
            ]}
          >
            <Input.Password placeholder="Confirm password" />
          </Form.Item>

          <Form.Item
            label="Date of Birth"
            name="dateOfBirth"
            rules={[{ required: true, message: 'Please select your date of birth' }]}
          >
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item
            label="Location"
            name="location"
            rules={[{ required: true, message: 'Please select your location' }]}
          >
            <Select
              placeholder="Select location"
              options={locationOptions}
            />
          </Form.Item>

          <Button
            block
            type="primary"
            size="large"
            htmlType="submit"
            style={{ background: '#674fa3', borderColor: '#674fa3', borderRadius: 10, fontWeight: 700 }}
          >
            Sign Up
          </Button>
        </Form>

        <div style={{ marginTop: 16, textAlign: 'center' }}>
          <Text style={{ color: '#666' }}>
            Already have an account?{' '}
            <Link to="/login" style={{ color: '#674fa3', fontWeight: 700 }}>
              Login
            </Link>
          </Text>
        </div>
      </Card>
    </div>
  );
};

export default Register;

