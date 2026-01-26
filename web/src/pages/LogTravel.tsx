import { Button, Card, Input, Space, Typography } from 'antd';
import { CarOutlined, EnvironmentOutlined } from '@ant-design/icons';
import { useMemo, useState } from 'react';

const { Title, Text } = Typography;

type TravelMode = 'car' | 'bus' | 'mrt' | 'plane' | 'motorbike' | 'bicycle_walk';

const inputBg = { background: '#F3F0FF' } as const;

const LogTravel = () => {
  const [mode, setMode] = useState<TravelMode>('car');

  const modes = useMemo(
    () => [
      { key: 'car' as const, label: 'Car', icon: <CarOutlined /> },
      { key: 'bus' as const, label: 'Bus', icon: <CarOutlined /> },
      { key: 'mrt' as const, label: 'Train/MRT', icon: <CarOutlined /> },
      { key: 'plane' as const, label: 'Plane', icon: <CarOutlined /> },
      { key: 'motorbike' as const, label: 'Motorbike', icon: <CarOutlined /> },
      { key: 'bicycle_walk' as const, label: 'Bicycle/Walk', icon: <CarOutlined /> },
    ],
    []
  );

  return (
    <div style={{ width: '100%', background: '#fff' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          Log Travel
        </Title>

        <div style={{ marginBottom: 18 }}>
          <Text style={{ fontWeight: 700, color: '#1f1f1f' }}>Select transport mode</Text>
          <div style={{ marginTop: 12, display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12 }}>
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
                    padding: 14,
                    cursor: 'pointer',
                    textAlign: 'left',
                    display: 'flex',
                    alignItems: 'center',
                    gap: 10,
                  }}
                >
                  <span style={{ color: active ? '#674fa3' : '#999', fontSize: 18 }}>{m.icon}</span>
                  <span style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.2 }}>
                    <span style={{ fontWeight: 800, color: '#1f1f1f' }}>{m.label}</span>
                  </span>
                </button>
              );
            })}
          </div>
        </div>

        <div style={{ marginTop: 10 }}>
          <Title level={4} style={{ margin: 0, marginBottom: 12 }}>
            Route Details
          </Title>

          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <Input
              prefix={<EnvironmentOutlined style={{ color: '#674fa3' }} />}
              placeholder="Origin (Start Point)"
              style={inputBg}
            />
            <Input
              prefix={<EnvironmentOutlined style={{ color: '#674fa3' }} />}
              placeholder="Destination (End Point)"
              style={inputBg}
            />

            <div
              style={{
                height: 260,
                borderRadius: 14,
                background: '#f5f5f5',
                border: '1px solid #f0f0f0',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#8c8c8c',
                fontWeight: 600,
              }}
            >
              Google Maps Integration (Placeholder)
            </div>

            <Button
              type="primary"
              size="large"
              style={{ background: '#674fa3', borderColor: '#674fa3', borderRadius: 12, fontWeight: 700 }}
            >
              Calculate Route
            </Button>
          </Space>
        </div>
      </Card>
    </div>
  );
};

export default LogTravel;

