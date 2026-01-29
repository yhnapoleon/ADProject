import { Button, Card, Form, Input, message, Typography } from 'antd';
import { useNavigate, Link } from 'react-router-dom';
import { useState } from 'react';
import splashIcon from '../assets/icons/splash.svg';
import request from '../utils/request';

const { Title, Text } = Typography;

type FormValues = {
  email: string;
  password: string;
};

type AuthResponse = {
  token: string;
  user: {
    id: string;
    username: string;
    email: string;
    nickname?: string;
  };
};

const Login = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const onFinish = async (values: FormValues) => {
    setLoading(true);
    try {
      const response: any = await request.post('/Auth/login', {
        email: values.email,
        password: values.password,
      });

      const token = response?.token ?? response?.Token ?? response?.accessToken ?? response?.AccessToken;
      if (!token) {
        message.error('Login failed: No token received');
        return;
      }

      localStorage.setItem('token', token);
      localStorage.setItem('isLoggedIn', 'true');
      if (response?.user) {
        localStorage.setItem('user', JSON.stringify(response.user));
      }

      message.success('Welcome back!');
      navigate('/dashboard', { replace: true });
    } catch (error: any) {
      console.error('Login error:', error);
      const errorMessage =
        error.response?.data?.error ||
        error.response?.data?.message ||
        error.message ||
        'Login failed. Please check your credentials.';
      message.error(errorMessage);
    } finally {
      setLoading(false);
    }
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
      <Card style={{ width: 'min(420px, 92vw)', borderRadius: 12, boxShadow: '0 2px 10px rgba(0,0,0,0.06)' }}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginBottom: 18 }}>
          <img src={splashIcon} alt="EcoLens" style={{ width: 64, height: 64 }} />
          <div style={{ marginTop: 10, fontWeight: 800, color: '#674fa3', fontSize: 20 }}>EcoLens</div>
          <Title level={3} style={{ marginTop: 10, marginBottom: 0 }}>
            Welcome Back
          </Title>
        </div>

        <Form<FormValues> layout="vertical" onFinish={onFinish} autoComplete="off">
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
            rules={[{ required: true, message: 'Please enter your password' }]}
          >
            <Input.Password placeholder="Enter password" />
          </Form.Item>

          <div style={{ marginTop: -8, marginBottom: 16 }}>
            <Link to="#" style={{ color: '#674fa3', fontSize: 14 }}>
              Forgot Password?
            </Link>
          </div>

          <Form.Item>
            <Button
              block
              type="primary"
              size="large"
              htmlType="submit"
              loading={loading}
              style={{ background: '#674fa3', borderColor: '#674fa3', borderRadius: 10, fontWeight: 700 }}
            >
              Log In
            </Button>
          </Form.Item>
        </Form>

        <div style={{ marginTop: 16, textAlign: 'center' }}>
          <Text style={{ color: '#666' }}>
            Don&apos;t have an account?{' '}
            <Link to="/register" style={{ color: '#674fa3', fontWeight: 700 }}>
              Register
            </Link>
          </Text>
        </div>
      </Card>
    </div>
  );
};

export default Login;

