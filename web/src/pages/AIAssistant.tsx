import { Button, Card, Input, Space, Typography } from 'antd';
import { SendOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;

const AIAssistant = () => {
  return (
    <div style={{ width: '100%' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          AI Eco-Assistant
        </Title>

        <div
          style={{
            background: '#f5f5f5',
            borderRadius: 12,
            padding: 16,
            minHeight: 360,
            border: '1px solid #f0f0f0',
          }}
        >
          <Text>Hello! I can help you analyze your carbon footprint tips.</Text>
        </div>

        <div style={{ marginTop: 16 }}>
          <Space.Compact style={{ width: '100%' }}>
            <Input placeholder="Type your message..." />
            <Button
              type="primary"
              icon={<SendOutlined />}
              style={{ background: '#674fa3', borderColor: '#674fa3' }}
            >
              Send
            </Button>
          </Space.Compact>
        </div>
      </Card>
    </div>
  );
};

export default AIAssistant;

