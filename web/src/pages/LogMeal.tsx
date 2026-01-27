import { Button, Card, Form, Input, InputNumber, message, Select, Space, Typography, Upload } from 'antd';
import type { UploadProps } from 'antd';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;

type FormValues = {
  foodName: string;
  amount: number;
  note?: string;
};

const FACTOR = 27.0; // Default emission factor

const inputBg = { background: '#F3F0FF' } as const;

const LogMeal = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm<FormValues>();
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const amount = Form.useWatch('amount', form);

  const emissions = useMemo(() => {
    const a = typeof amount === 'number' ? amount : Number(amount);
    if (!a || Number.isNaN(a)) return null;
    return a * FACTOR;
  }, [amount]);

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

  const handleSave = async () => {
    await form.validateFields();
    message.success('Meal logged successfully!');
    navigate('/dashboard');
  };

  return (
    <div style={{ width: '100%', background: '#fff' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          Log Meal
        </Title>

        {/* Photo upload */}
        <Upload accept="image/*" showUploadList={false} beforeUpload={beforeUpload}>
          <div
            style={{
              border: '2px dashed #674fa3',
              background: '#F3F0FF',
              borderRadius: 16,
              padding: 28,
              cursor: 'pointer',
              position: 'relative',
              overflow: 'hidden',
              minHeight: 320,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: '100%',
            }}
          >
            {!previewUrl ? (
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontWeight: 800, color: '#674fa3', fontSize: 18 }}>Add photo</div>
                <div style={{ color: '#8c8c8c', fontSize: 12, marginTop: 6 }}>
                  Tap to choose an image, or drag & drop here
                </div>
                <Button
                  type="primary"
                  style={{ marginTop: 14, background: '#674fa3', borderColor: '#674fa3', borderRadius: 999 }}
                >
                  Choose Photo
                </Button>
              </div>
            ) : (
              <img
                src={previewUrl}
                alt="Meal preview"
                style={{ width: '100%', maxHeight: 420, objectFit: 'cover', borderRadius: 12 }}
              />
            )}
          </div>
        </Upload>

        {/* Form */}
        <Form<FormValues>
          form={form}
          layout="vertical"
          style={{ marginTop: 18 }}
        >
            <Form.Item
              label="Food name"
              name="foodName"
              rules={[{ required: true, message: 'Please enter food name' }]}
            >
              <Input placeholder="e.g., Beef burger / Rice / Salad" style={inputBg} />
            </Form.Item>

            <Form.Item
              label="Amount"
              name="amount"
              rules={[{ required: true, message: 'Please enter amount' }]}
            >
              <InputNumber
                style={{ width: '100%', ...inputBg }}
                min={0}
                step={0.1}
                placeholder="e.g., 0.25"
                addonAfter="kg"
              />
            </Form.Item>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <Form.Item label="Emission factor (kg CO2e/kg)">
                <Input readOnly style={inputBg} />
              </Form.Item>
              <Form.Item label="Carbon Emissions (kg)">
                <Input
                  readOnly
                  style={inputBg}
                />
              </Form.Item>
            </div>

            <Form.Item label="Note (optional)" name="note">
              <Input.TextArea placeholder="e.g., dinner / canteen / brand" style={inputBg} rows={3} />
            </Form.Item>

            <Space direction="vertical" size={10} style={{ width: '100%' }}>
              <Button
                block
                type="primary"
                size="large"
                onClick={handleSave}
                style={{ background: '#674fa3', borderColor: '#674fa3', borderRadius: 12, fontWeight: 700 }}
              >
                Save
              </Button>
              <Text style={{ fontSize: 12, color: '#8c8c8c' }}>
                This adds a food record into your carbon ledger (stored locally)...
              </Text>
            </Space>
        </Form>
      </Card>
    </div>
  );
};

export default LogMeal;

