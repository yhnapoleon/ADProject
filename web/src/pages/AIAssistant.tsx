import { Button, Card, Input, Space, Typography } from 'antd';
import { AppleFilled, BulbFilled, CarFilled, SendOutlined } from '@ant-design/icons';
import { useMemo, useState } from 'react';

const { Title, Text } = Typography;

type ChatMessage = {
  id: string;
  role: 'user' | 'assistant';
  content: string;
};

const AIAssistant = () => {
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: 'welcome',
      role: 'assistant',
      content: 'Hello! I can help you analyze your carbon footprint tips.',
    },
  ]);

  const suggestions = useMemo(
    () => [
      {
        key: 'food',
        label: 'Food Tips',
        icon: <AppleFilled style={{ color: '#ff4d4f' }} />,
        background: '#F3F0FF',
        message: 'Give me some food carbon reduction tips.',
      },
      {
        key: 'transport',
        label: 'Transport',
        icon: <CarFilled style={{ color: '#08979c' }} />,
        background: '#E6F7FF',
        message: 'How can I reduce my transport emissions?',
      },
      {
        key: 'utilities',
        label: 'Utilities',
        icon: <BulbFilled style={{ color: '#faad14' }} />,
        background: '#FFF7E6',
        message: 'Share some utilities saving tips to lower my footprint.',
      },
    ],
    []
  );

  const sendMessage = (content: string) => {
    const trimmed = content.trim();
    if (!trimmed) return;
    setMessages((prev) => [
      ...prev,
      { id: `${Date.now()}-u`, role: 'user', content: trimmed },
    ]);
    setInput('');
  };

  return (
    <div style={{ width: '100%' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          Carbon AI Assistant
        </Title>

        <div
          style={{
            background: '#f5f5f5',
            borderRadius: 12,
            padding: 16,
            minHeight: 360,
            border: '1px solid #f0f0f0',
            display: 'flex',
            flexDirection: 'column',
            gap: 12,
          }}
        >
          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
            {suggestions.map((s) => (
              <Button
                key={s.key}
                onClick={() => sendMessage(s.message)}
                icon={s.icon}
                style={{
                  borderRadius: 999,
                  background: s.background,
                  borderColor: 'transparent',
                  color: '#1f1f1f',
                  fontWeight: 600,
                }}
              >
                {s.label}
              </Button>
            ))}
          </div>

          <div style={{ flex: 1, overflowY: 'auto', paddingRight: 4 }}>
            <Space direction="vertical" size={10} style={{ width: '100%' }}>
              {messages.map((m) => (
                <div
                  key={m.id}
                  style={{
                    display: 'flex',
                    justifyContent: m.role === 'user' ? 'flex-end' : 'flex-start',
                  }}
                >
                  <div
                    style={{
                      maxWidth: 560,
                      padding: '10px 12px',
                      borderRadius: 12,
                      background: m.role === 'user' ? '#674fa3' : '#ffffff',
                      color: m.role === 'user' ? '#ffffff' : '#1f1f1f',
                      border: m.role === 'user' ? 'none' : '1px solid #f0f0f0',
                      boxShadow: m.role === 'user' ? '0 2px 8px rgba(103, 79, 163, 0.18)' : 'none',
                      whiteSpace: 'pre-wrap',
                      lineHeight: 1.45,
                    }}
                  >
                    <Text style={{ color: 'inherit' }}>{m.content}</Text>
                  </div>
                </div>
              ))}
            </Space>
          </div>
        </div>

        <div style={{ marginTop: 16 }}>
          <Space.Compact style={{ width: '100%' }}>
            <Input
              placeholder="Type your message..."
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onPressEnter={() => sendMessage(input)}
            />
            <Button
              type="primary"
              icon={<SendOutlined />}
              style={{ background: '#674fa3', borderColor: '#674fa3' }}
              onClick={() => sendMessage(input)}
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

