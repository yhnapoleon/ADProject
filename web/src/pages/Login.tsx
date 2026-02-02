import { Button, Card, Form, Input, message, Typography } from 'antd';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useState, useEffect } from 'react';
import splashIcon from '../assets/icons/splash.svg';
import request from '../utils/request';
import SplashScreen from '../components/SplashScreen';
import Onboarding from './Onboarding';

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
  const location = useLocation();
  const [loading, setLoading] = useState(false);
  const [stage, setStage] = useState<'splash' | 'onboarding' | 'login'>(() =>
    (location.state as { fromRegister?: boolean })?.fromRegister ? 'login' : 'splash'
  );

  useEffect(() => {
    if (stage === 'splash') {
      const timer = setTimeout(() => {
        setStage('onboarding');
      }, 2500);
      return () => clearTimeout(timer);
    }
  }, [stage]);

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

      const resp = error.response;
      const respData = resp?.data;
      const serverMsg = (respData?.error || respData?.message || '') as string;

      // Prefer HTTP status: 401 → user is banned; 403/404/msg → other cases
      if (resp) {
        if (resp.status === 401) {
          message.error('User is banned');
        } else if (resp.status === 404 || /not\s+found|no\s+account|user\s+not\s+found/i.test(serverMsg)) {
          message.error('No account found with this email');
        } else if (resp.status === 403 || /banned|disabled|forbidden/i.test(serverMsg)) {
          message.error('Your account has been banned');
        } else if (/invalid|incorrect|password|credentials/i.test(serverMsg)) {
          message.error('Incorrect email or password');
        } else {
          const fallback = serverMsg || error.message || 'Login failed. Please check your credentials.';
          message.error(fallback);
        }
      } else {
        // 非 HTTP 响应错误（网络等）
        message.error(error.message || 'Login failed. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  if (stage === 'splash') {
    return <SplashScreen />;
  }

  if (stage === 'onboarding') {
    return <Onboarding onFinish={() => setStage('login')} />;
  }

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

