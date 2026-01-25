import { Button, Card, Form, Input, message, Typography } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import splashIcon from '../assets/icons/splash.svg';

const { Title, Text } = Typography;

type FormValues = {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
};

const Register = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm<FormValues>();

  const onFinish = (_values: FormValues) => {
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

