import { Button, Card, Input, Space, Typography, Form, message } from 'antd';
import { EnvironmentOutlined } from '@ant-design/icons';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import request from '../utils/request';

const { Title, Text } = Typography;

const inputBg = { background: '#F3F0FF' } as const;

/** Backend TransportMode enum values (Motorcycle uses ElectricBike = 2) */
const TRANSPORT_MODE_IDS: Record<string, number> = {
  Plane: 9,
  Bus: 4,
  Bicycle: 1,
  ElectricBike: 2,
  CarGasoline: 6,
  Ship: 8,
  Subway: 3,
  CarElectric: 7,
};

/** Display label and icon; key can be backend LabelName or display key (CarGasolineExtra, Motorcycle) */
const TRANSPORT_META: Record<string, { displayLabel: string; icon: string }> = {
  Plane: { displayLabel: 'Airplane', icon: '✈️' },
  Bus: { displayLabel: 'Bus', icon: '🚌' },
  Bicycle: { displayLabel: 'Cycle', icon: '🚲' },
  CarGasoline: { displayLabel: 'Car', icon: '🚗' },
  Ship: { displayLabel: 'Ship', icon: '🚢' },
  Subway: { displayLabel: 'MRT', icon: '🚇' },
  Motorcycle: { displayLabel: 'Motorcycle', icon: '🏍️' },
  CarElectric: { displayLabel: 'Car (electric)', icon: '⚡' },
  CarGasolineExtra: { displayLabel: 'Car (gasoline)', icon: '⛽' },
};

/** Hardcoded emission factors (kg CO₂e/km) for display in Select transport mode */
const EMISSION_FACTORS: Record<string, number> = {
  Plane: 0.25,
  Bus: 0.05,
  Bicycle: 0,
  Ship: 0.03,
  Subway: 0.03,
  Motorcycle: 0.02,
  CarElectric: 0.05,
  CarGasolineExtra: 0.2,
};

/** Display order (Car option removed); 4 per row */
const DISPLAY_ORDER: { labelName: string; key: string }[] = [
  { labelName: 'Plane', key: 'Plane' },
  { labelName: 'Bus', key: 'Bus' },
  { labelName: 'Bicycle', key: 'Bicycle' },
  { labelName: 'Ship', key: 'Ship' },
  { labelName: 'Subway', key: 'Subway' },
  { labelName: 'ElectricBike', key: 'Motorcycle' },
  { labelName: 'CarElectric', key: 'CarElectric' },
  { labelName: 'CarGasoline', key: 'CarGasolineExtra' },
];

type TransportOption = {
  labelName: string;
  displayLabel: string;
  co2Factor: number;
  transportMode: number;
  icon: string;
  key: string;
};

const LogTravel = () => {
  const navigate = useNavigate();
  const [selectedMode, setSelectedMode] = useState<TransportOption | null>(null);
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const transportOptions = useMemo<TransportOption[]>(() =>
    DISPLAY_ORDER.map(({ labelName, key }) => {
      const metaKey = (key === 'Motorcycle' || key === 'CarGasolineExtra') ? key : labelName;
      const meta = TRANSPORT_META[metaKey] ?? { displayLabel: labelName, icon: '🚗' };
      const co2Factor = EMISSION_FACTORS[key] ?? 0;
      const transportMode = TRANSPORT_MODE_IDS[labelName] ?? (key === 'CarGasolineExtra' ? 6 : 0);
      return { labelName: key, displayLabel: meta.displayLabel, co2Factor, transportMode, icon: meta.icon, key };
    }),
    []
  );

  const currentFactor = selectedMode ? selectedMode.co2Factor : 0;

  return (
    <div style={{ width: '100%', background: '#fff' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          Log Travel
        </Title>

        <div style={{ marginBottom: 18 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
            <Text style={{ fontSize: '20px', color: '#1f1f1f' }}>Select transport mode</Text>
            {selectedMode && (
              <Text style={{ color: '#ff4d4f', fontSize: '20px' }}>
                Emission Factor: {currentFactor.toFixed(3)} kg CO₂e/km
              </Text>
            )}
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
              {transportOptions.map((m) => {
                const active = selectedMode?.key === m.key;
                return (
                  <button
                    key={m.key}
                    type="button"
                    onClick={() => setSelectedMode(m)}
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
                    <span style={{ fontWeight: 800, color: '#1f1f1f', fontSize: 14 }}>{m.displayLabel}</span>
                  </button>
                );
              })}
            </div>
        </div>

        <Form form={form} layout="vertical" onFinish={async (values) => {
          // Validation
          if (!selectedMode) {
            message.error('Please select a transport mode');
            return;
          }
          if (!values.origin || !values.destination) {
            message.error('Please fill in both Origin and Destination');
            return;
          }

          setLoading(true);
          try {
            const response = await request.post('/api/travel', {
              originAddress: values.origin,
              destinationAddress: values.destination,
              transportMode: selectedMode.transportMode,
              notes: values.note || null,
            });

            message.success('Travel logged successfully!');
            console.log('Travel log created:', response);
            navigate('/dashboard');
          } catch (error: any) {
            console.error('Failed to log travel:', error);
            const errorMessage = error.response?.data?.error || 'Failed to log travel. Please try again.';
            message.error(errorMessage);
          } finally {
            setLoading(false);
          }
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
                loading={loading}
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

