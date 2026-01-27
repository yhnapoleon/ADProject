import { Button, Card, DatePicker, Form, InputNumber, Space, Typography, Upload, Input, message } from 'antd';
import type { UploadProps } from 'antd';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;

const inputBg = { background: '#F3F0FF' } as const;

type FormValues = {
  electricityUsage?: number;
  waterUsage?: number;
  month?: any;
  note?: string;
};

const LogUtility = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm<FormValues>();
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const fileToBase64 = (file: File) =>
    new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });

  const beforeUpload: UploadProps['beforeUpload'] = async (file) => {
    const base64 = await fileToBase64(file as unknown as File);
    setPreviewUrl(base64);
    return false;
  };

  return (
    <div style={{ width: '100%', background: '#fff' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          Log Utility
        </Title>

        {/* Bill scan */}
        <Upload accept="image/*,.pdf" showUploadList={false} beforeUpload={beforeUpload}>
          <div
            style={{
              border: '2px dashed #674fa3',
              background: '#F3F0FF',
              borderRadius: 16,
              padding: 28,
              cursor: 'pointer',
              minHeight: 320,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              overflow: 'hidden',
              width: '100%',
            }}
          >
            {!previewUrl ? (
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontWeight: 800, color: '#674fa3', fontSize: 18 }}>Scan utility bill</div>
                <div style={{ color: '#8c8c8c', fontSize: 12, marginTop: 6 }}>
                  Upload your electricity/water/gas bill for auto-filling
                </div>
                <Button
                  type="primary"
                  style={{ marginTop: 14, background: '#674fa3', borderColor: '#674fa3', borderRadius: 999 }}
                >
                  Upload Bill
                </Button>
              </div>
            ) : (
              <img
                src={previewUrl}
                alt="Bill preview"
                style={{ width: '100%', maxHeight: 420, objectFit: 'cover', borderRadius: 12 }}
              />
            )}
          </div>
        </Upload>

        <Form<FormValues> form={form} layout="vertical" style={{ marginTop: 18 }} onFinish={async () => {
          message.success('Utility logged successfully!');
          navigate('/dashboard');
        }}>
          <Title level={4} style={{ marginTop: 0 }}>
            Manual Entry
          </Title>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <Form.Item 
              label="Electricity - Usage (kWh)" 
              name="electricityUsage"
              rules={[{ required: true, message: 'Please enter electricity usage' }]}
            >
              <InputNumber style={{ width: '100%', ...inputBg }} min={0} />
            </Form.Item>

            <Form.Item 
              label="Water - Usage (Cu M)" 
              name="waterUsage"
              rules={[{ required: true, message: 'Please enter water usage' }]}
            >
              <InputNumber style={{ width: '100%', ...inputBg }} min={0} />
            </Form.Item>
          </div>

          <Form.Item 
            label="Month" 
            name="month"
            rules={[{ required: true, message: 'Please select month' }]}
          >
            <DatePicker picker="month" style={{ width: '100%', ...inputBg }} />
          </Form.Item>

          <Form.Item label="Note (optional)" name="note">
            <Input.TextArea
              placeholder="Add any additional notes..."
              rows={3}
              style={inputBg}
            />
          </Form.Item>

          <Space direction="vertical" size={10} style={{ width: '100%' }}>
            <Button
              block
              type="primary"
              size="large"
              htmlType="submit"
              style={{ background: '#674fa3', borderColor: '#674fa3', borderRadius: 12, fontWeight: 700 }}
            >
              Save
            </Button>
            <Text style={{ fontSize: 12, color: '#8c8c8c' }}>
              This adds a utility record into your carbon ledger (stored locally)...
            </Text>
          </Space>
        </Form>
      </Card>
    </div>
  );
};

export default LogUtility;

