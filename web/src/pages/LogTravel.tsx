import { Button, Card, Input, Space, Typography, Form, message } from 'antd';
import { CarOutlined, EnvironmentOutlined, ThunderboltOutlined } from '@ant-design/icons';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;

type TravelMode = 'airplane' | 'bus' | 'cycle' | 'car' | 'ship' | 'mrt';

const inputBg = { background: '#F3F0FF' } as const;

const EMISSION_FACTORS: Record<TravelMode, number> = {
  airplane: 0.255,
  bus: 0.089,
  cycle: 0.0,
  car: 0.171,
  ship: 0.019,
  mrt: 0.041,
};

const LogTravel = () => {
  const navigate = useNavigate();
  const [mode, setMode] = useState<TravelMode | null>(null);
  const [form] = Form.useForm();

  const modes = useMemo(
    () => [
      { key: 'airplane' as const, label: 'Airplane', icon: '✈️' },
      { key: 'bus' as const, label: 'Bus', icon: '🚌' },
      { key: 'cycle' as const, label: 'Cycle', icon: '🚲' },
      { key: 'car' as const, label: 'Car', icon: '🚗' },
      { key: 'ship' as const, label: 'Ship', icon: '🚢' },
      { key: 'mrt' as const, label: 'MRT', icon: '🚇' },
    ],
    []
  );

  const currentFactor = mode ? EMISSION_FACTORS[mode] : 0;

  return (
    <div style={{ width: '100%', background: '#fff' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          Log Travel
        </Title>

        <div style={{ marginBottom: 18 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
            <Text style={{ fontSize: '20px', color: '#1f1f1f' }}>Select transport mode</Text>
            {mode && (
              <Text style={{ color: '#ff4d4f', fontSize: '20px' }}>
                Emission Factor: {currentFactor.toFixed(3)} kg CO₂e/km
              </Text>
            )}
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: 12 }}>
            {modes.map((m) => {
              const active = mode === m.key;
              return (
                <button
                  key={m.key}
                  type="button"
                  onClick={() => setMode(m.key)}
                  style={{
                    borderRadius: 14,
                    border: `2px solid ${active ? '#674fa3' : '#f0f0f0'}`,
                    background: active ? '#F3F0FF' : '#fff',
                    padding: 16,
                    cursor: 'pointer',
                    textAlign: 'center',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    gap: 8,
                  }}
                >
                  <span style={{ fontSize: 32 }}>{m.icon}</span>
                  <span style={{ fontWeight: 800, color: '#1f1f1f', fontSize: 14 }}>{m.label}</span>
                </button>
              );
            })}
          </div>
        </div>

        <Form form={form} layout="vertical" onFinish={async (values) => {
          // Validation
          if (!mode) {
            message.error('Please select a transport mode');
            return;
          }
          if (!values.origin || !values.destination) {
            message.error('Please fill in both Origin and Destination');
            return;
          }
          message.success('Travel logged successfully!');
          navigate('/dashboard');
        }}>
          <div style={{ marginTop: 10 }}>
            <Title level={4} style={{ margin: 0, marginBottom: 12 }}>
              Route Details
            </Title>

            <Space direction="vertical" size={12} style={{ width: '100%' }}>
              <Form.Item
                name="origin"
                rules={[{ required: true, message: 'Please enter origin' }]}
              >
                <Input
                  prefix={<EnvironmentOutlined style={{ color: '#674fa3' }} />}
                  placeholder="Origin (Start Point)"
                  style={inputBg}
                />
              </Form.Item>
              <Form.Item
                name="destination"
                rules={[{ required: true, message: 'Please enter destination' }]}
              >
                <Input
                  prefix={<EnvironmentOutlined style={{ color: '#674fa3' }} />}
                  placeholder="Destination (End Point)"
                  style={inputBg}
                />
              </Form.Item>

              <iframe
                src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3988.7975!2d103.8198!3d1.3521!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x0%3A0x0!2zMcKwMjEnMDcuNiJOIDEwM8KwNDknMTEuMyJF!5e0!3m2!1sen!2ssg!4v1234567890"
                width="100%"
                height="260"
                style={{ border: 0, borderRadius: 14 }}
                allowFullScreen
                loading="lazy"
                referrerPolicy="no-referrer-when-downgrade"
              />

              <Form.Item label="Note (optional)" name="note">
                <Input.TextArea
                  placeholder="Add any additional notes..."
                  rows={3}
                  style={inputBg}
                />
              </Form.Item>

              <Button
                block
                type="primary"
                size="large"
                htmlType="submit"
                style={{ background: '#674fa3', borderColor: '#674fa3', borderRadius: 12, fontWeight: 700 }}
              >
                Save
              </Button>
            </Space>
          </div>
        </Form>
      </Card>
    </div>
  );
};

export default LogTravel;

