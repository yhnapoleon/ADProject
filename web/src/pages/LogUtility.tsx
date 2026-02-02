import { Button, Card, DatePicker, Form, InputNumber, Space, Typography, Upload, Input, message } from 'antd';
import type { UploadProps } from 'antd';
import dayjs from 'dayjs';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import request from '../utils/request';

const { Title, Text } = Typography;

const inputBg = { background: '#F3F0FF' } as const;

/** 后端账单类型：0 电 1 水 2 燃气 3 综合 */
const BILL_TYPE_COMBINED = 3;

/** 上传/识别返回的账单数据（与后端 UtilityBillResponseDto 对齐） */
type UtilityBillResponse = {
  id?: number;
  billType: number;
  billPeriodStart: string;
  billPeriodEnd: string;
  electricityUsage?: number | null;
  waterUsage?: number | null;
  gasUsage?: number | null;
};

type FormValues = {
  electricityUsage?: number;
  waterUsage?: number;
  gasUsage?: number;
  month?: dayjs.Dayjs;
  note?: string;
};

const LogUtility = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm<FormValues>();
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [uploadLoading, setUploadLoading] = useState(false);
  const [submitLoading, setSubmitLoading] = useState(false);

  const fileToBase64 = (file: File) =>
    new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });

  const beforeUpload: UploadProps['beforeUpload'] = async (file) => {
    const rawFile = file as unknown as File;
    const base64 = await fileToBase64(rawFile);
    setPreviewUrl(base64);

    const formData = new FormData();
    formData.append('file', rawFile);
    setUploadLoading(true);
    try {
      const res = await request.post<UtilityBillResponse>('/api/UtilityBill/upload', formData);
      form.setFieldsValue({
        electricityUsage: res?.electricityUsage ?? undefined,
        waterUsage: res?.waterUsage ?? undefined,
        gasUsage: res?.gasUsage ?? undefined,
        // 使用 billPeriodEnd 来设置月份，因为账单属于结束日期所在的月份
        month: res?.billPeriodEnd ? dayjs(res.billPeriodEnd) : undefined,
      });
      message.success('Bill recognized. Please review and save.');
    } catch (err: any) {
      console.error('[LogUtility] Upload failed:', err?.response?.status, err?.response?.data);
      const msg = err?.response?.data?.error ?? err?.message ?? 'Recognition failed. Please enter manually.';
      message.error(msg);
    } finally {
      setUploadLoading(false);
    }
    return false;
  };

  return (
    <div style={{ width: '100%', background: '#fff' }}>
      <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
          Log Utility
        </Title>

        {/* Bill scan */}
        <Upload accept="image/*,.pdf" showUploadList={false} beforeUpload={beforeUpload} disabled={uploadLoading}>
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
                  loading={uploadLoading}
                  style={{ marginTop: 14, background: '#674fa3', borderColor: '#674fa3', borderRadius: 999 }}
                >
                  {uploadLoading ? 'Recognizing...' : 'Upload Bill'}
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

        <Form<FormValues> form={form} layout="vertical" style={{ marginTop: 18 }} onFinish={async (values) => {
          const month = values.month;
          if (!month) {
            message.error('Please select bill month.');
            return;
          }
          const billPeriodStart = month.startOf('month').format('YYYY-MM-DD');
          const billPeriodEnd = month.endOf('month').format('YYYY-MM-DD');
          setSubmitLoading(true);
          try {
            await request.post('/api/UtilityBill/manual', {
              billType: BILL_TYPE_COMBINED,
              billPeriodStart,
              billPeriodEnd,
              electricityUsage: values.electricityUsage ?? null,
              waterUsage: values.waterUsage ?? null,
              gasUsage: values.gasUsage ?? null,
            });
            message.success('Utility bill saved.');
            navigate('/dashboard');
          } catch (err: any) {
            console.error('[LogUtility] Save failed:', err?.response?.status, err?.response?.data);
            const msg = err?.response?.data?.error ?? err?.message ?? 'Save failed. Please try again.';
            message.error(msg);
          } finally {
            setSubmitLoading(false);
          }
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

            <Form.Item 
              label="Gas - Usage (kWh or Cu M, optional)" 
              name="gasUsage"
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
              loading={submitLoading}
              disabled={uploadLoading}
              style={{ background: '#674fa3', borderColor: '#674fa3', borderRadius: 12, fontWeight: 700 }}
            >
              Save
            </Button>
            <Text style={{ fontSize: 12, color: '#8c8c8c' }}>
              This will be added to your carbon ledger and included in stats.
            </Text>
          </Space>
        </Form>
      </Card>
    </div>
  );
};

export default LogUtility;

