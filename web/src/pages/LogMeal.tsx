import { Button, Card, Form, Input, InputNumber, message, Space, Typography, Upload, Spin } from 'antd';
import type { UploadProps } from 'antd';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { BrowserMultiFormatReader } from '@zxing/library';
import request from '../utils/request';

const { Title, Text } = Typography;

type FormValues = {
  foodName: string;
  amount: number;
  co2Factor?: number;
  note?: string;
};

type VisionResponse = {
  label: string;
  confidence: number;
  sourceModel: string;
};

type BarcodeResponse = {
  id: number;
  barcode: string;
  productName: string;
  carbonReferenceLabel?: string;
  co2Factor?: number;
  unit?: string;
  source?: string;
  category?: string;
  brand?: string;
};

const inputBg = { background: '#F3F0FF' } as const;

const LogMeal = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm<FormValues>();
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [detectionType, setDetectionType] = useState<'barcode' | 'food' | null>(null);
  const [detectedInfo, setDetectedInfo] = useState<{ type: 'barcode' | 'food'; data: BarcodeResponse | VisionResponse } | null>(null);

  // 注意：路由保护已经在 App.tsx 中通过 RequireUserAuth 处理
  // 这里不需要再次检查，避免与路由保护冲突导致循环跳转

  const amount = Form.useWatch('amount', form);
  const formCo2Factor = Form.useWatch('co2Factor', form);
  const co2Factor = formCo2Factor || (detectedInfo?.type === 'barcode' 
    ? (detectedInfo.data as BarcodeResponse).co2Factor 
    : null);

  const emissions = useMemo(() => {
    const a = typeof amount === 'number' ? amount : Number(amount);
    if (!a || Number.isNaN(a) || !co2Factor) return null;
    return a * co2Factor;
  }, [amount, co2Factor]);

  const fileToBase64 = (file: File) =>
    new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });

  const fileToImage = (file: File) =>
    new Promise<HTMLImageElement>((resolve, reject) => {
      const img = new Image();
      img.onload = () => resolve(img);
      img.onerror = reject;
      img.src = URL.createObjectURL(file);
    });

  // 尝试从图片中识别条形码
  const scanBarcode = async (file: File): Promise<string | null> => {
    let imageUrl: string | null = null;
    try {
      const codeReader = new BrowserMultiFormatReader();
      const img = await fileToImage(file);
      imageUrl = img.src;
      
      // 使用图片元素直接识别条形码
      const result = await codeReader.decodeFromImageElement(img);
      
      if (result && result.getText()) {
        return result.getText();
      }
      return null;
    } catch (error) {
      // 条形码识别失败，返回null
      console.log('Barcode scan failed:', error);
      return null;
    } finally {
      // 确保清理URL对象
      if (imageUrl) {
        URL.revokeObjectURL(imageUrl);
      }
    }
  };

  // 调用条形码API
  const fetchBarcodeInfo = async (barcode: string): Promise<BarcodeResponse | null> => {
    try {
      const response: any = await request.get(`/barcode/${barcode}`);
      // request 拦截器已经返回了 response.data，所以直接使用
      return response as BarcodeResponse;
    } catch (error: any) {
      console.error('Barcode API error:', error);
      if (error.response?.status === 401) {
        // 401 错误由 beforeUpload 统一处理，这里只返回 null
        return null;
      }
      if (error.response?.status === 404) {
        message.warning('条形码未找到，将尝试食物识别');
      } else {
        message.error('条形码查询失败');
      }
      return null;
    }
  };

  // 调用食物识别API
  const recognizeFood = async (file: File): Promise<VisionResponse | null> => {
    try {
      const formData = new FormData();
      formData.append('image', file);
      
      // 不要手动设置 Content-Type，让浏览器自动设置（包括 boundary）
      const response: any = await request.post('/vision/analyze', formData);
      // request 拦截器已经返回了 response.data，所以直接使用
      return response as VisionResponse;
    } catch (error: any) {
      console.error('Food recognition error:', error);
      if (error.response?.status === 401) {
        // 401 错误由 beforeUpload 统一处理，这里只返回 null
        return null;
      }
      message.error('食物识别失败');
      return null;
    }
  };

  const beforeUpload: UploadProps['beforeUpload'] = async (file) => {
    // 清除之前的错误消息
    message.destroy('detecting');
    
    // 检查 token 是否存在（用于 API 调用）
    const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
    if (!token) {
      message.error('登录已过期，请重新登录');
      // 清除登录状态
      localStorage.removeItem('isLoggedIn');
      setTimeout(() => {
        navigate('/login');
      }, 1500);
      return false;
    }
    
    const base64 = await fileToBase64(file as unknown as File);
    setPreviewUrl(base64);
    setLoading(true);
    setDetectionType(null);
    setDetectedInfo(null);
    
    try {
      // 先尝试识别条形码
      const barcode = await scanBarcode(file as unknown as File);
      
      if (barcode) {
        setDetectionType('barcode');
        message.loading({ content: '识别到条形码，正在查询产品信息...', key: 'detecting', duration: 0 });
        
        const barcodeInfo = await fetchBarcodeInfo(barcode);
        if (barcodeInfo) {
          setDetectedInfo({ type: 'barcode', data: barcodeInfo });
          // 自动填充表单
          form.setFieldsValue({
            foodName: barcodeInfo.productName || barcodeInfo.carbonReferenceLabel || '',
            co2Factor: barcodeInfo.co2Factor || undefined,
          });
          message.success({ content: `识别成功：${barcodeInfo.productName || barcodeInfo.carbonReferenceLabel}`, key: 'detecting' });
        } else {
          // 检查 token 是否被清除（表示 401 错误）
          const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
          if (!token) {
            // 401 错误，显示提示并跳转
            message.error({ content: '登录已过期，请重新登录', key: 'detecting' });
            setLoading(false);
            setTimeout(() => {
              navigate('/login');
            }, 2000);
            return false;
          }
          
          // 条形码查询失败，尝试食物识别
          message.loading({ content: '条形码查询失败，尝试食物识别...', key: 'detecting' });
          const foodInfo = await recognizeFood(file as unknown as File);
          if (foodInfo) {
            setDetectionType('food');
            setDetectedInfo({ type: 'food', data: foodInfo });
            form.setFieldsValue({
              foodName: foodInfo.label,
            });
            message.success({ content: `识别为：${foodInfo.label}`, key: 'detecting' });
          } else {
            // 再次检查 token 是否被清除（表示 401 错误）
            const tokenAfterFood = localStorage.getItem('token') || localStorage.getItem('adminToken');
            if (!tokenAfterFood) {
              // 401 错误，显示提示并跳转
              message.error({ content: '登录已过期，请重新登录', key: 'detecting' });
              setLoading(false);
              setTimeout(() => {
                navigate('/login');
              }, 2000);
              return false;
            }
            // 食物识别也失败，显示提示
            message.warning({ content: '未能识别图片内容，请手动输入', key: 'detecting' });
          }
        }
      } else {
        // 没有识别到条形码，尝试食物识别
        setDetectionType('food');
        message.loading({ content: '未识别到条形码，正在识别食物...', key: 'detecting' });
        
        const foodInfo = await recognizeFood(file as unknown as File);
        if (foodInfo) {
          setDetectedInfo({ type: 'food', data: foodInfo });
          form.setFieldsValue({
            foodName: foodInfo.label,
          });
          message.success({ content: `识别为：${foodInfo.label}`, key: 'detecting' });
        } else {
          // 检查 token 是否被清除（表示 401 错误）
          const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
          if (!token) {
            // 401 错误，显示提示并跳转
            message.error({ content: '登录已过期，请重新登录', key: 'detecting' });
            setLoading(false);
            setTimeout(() => {
              navigate('/login');
            }, 2000);
            return false;
          }
          message.warning({ content: '未能识别图片内容，请手动输入', key: 'detecting' });
        }
      }
    } catch (error: any) {
      console.error('Detection error:', error);
      if (error.response?.status === 401) {
        message.error({ content: '登录已过期，请重新登录', key: 'detecting' });
        setTimeout(() => {
          navigate('/login');
        }, 2000);
      } else {
        message.error({ content: '识别过程出错', key: 'detecting' });
      }
    } finally {
      setLoading(false);
    }
    
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
        <Upload accept="image/*" showUploadList={false} beforeUpload={beforeUpload} disabled={loading}>
          <div
            style={{
              border: '2px dashed #674fa3',
              background: '#F3F0FF',
              borderRadius: 16,
              padding: 28,
              cursor: loading ? 'wait' : 'pointer',
              position: 'relative',
              overflow: 'hidden',
              minHeight: 320,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: '100%',
            }}
          >
            {loading ? (
              <div style={{ textAlign: 'center' }}>
                <Spin size="large" />
                <div style={{ marginTop: 16, color: '#674fa3', fontSize: 14 }}>
                  正在识别图片...
                </div>
              </div>
            ) : !previewUrl ? (
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontWeight: 800, color: '#674fa3', fontSize: 18 }}>Add photo</div>
                <div style={{ color: '#8c8c8c', fontSize: 12, marginTop: 6 }}>
                  Tap to choose an image, or drag & drop here
                </div>
                <div style={{ color: '#8c8c8c', fontSize: 11, marginTop: 4 }}>
                  (支持条形码和食物识别)
                </div>
                <Button
                  type="primary"
                  style={{ marginTop: 14, background: '#674fa3', borderColor: '#674fa3', borderRadius: 999 }}
                >
                  Choose Photo
                </Button>
              </div>
            ) : (
              <div style={{ position: 'relative', width: '100%' }}>
                <img
                  src={previewUrl}
                  alt="Meal preview"
                  style={{ width: '100%', maxHeight: 420, objectFit: 'cover', borderRadius: 12 }}
                />
                {detectedInfo && (
                  <div
                    style={{
                      position: 'absolute',
                      top: 8,
                      right: 8,
                      background: detectionType === 'barcode' ? '#52c41a' : '#1890ff',
                      color: 'white',
                      padding: '4px 12px',
                      borderRadius: 12,
                      fontSize: 12,
                      fontWeight: 600,
                    }}
                  >
                    {detectionType === 'barcode' ? '📷 条形码' : '🍽️ 食物'}
                  </div>
                )}
              </div>
            )}
          </div>
        </Upload>

        {/* Detection result info */}
        {detectedInfo && detectedInfo.type === 'barcode' && (
          <Card
            size="small"
            style={{
              marginTop: 12,
              background: '#f6ffed',
              border: '1px solid #b7eb8f',
            }}
          >
            <div style={{ fontSize: 13 }}>
              <div style={{ fontWeight: 600, marginBottom: 4 }}>
                📦 产品信息
              </div>
              <div style={{ color: '#595959' }}>
                产品名称: {(detectedInfo.data as BarcodeResponse).productName || (detectedInfo.data as BarcodeResponse).carbonReferenceLabel}
              </div>
              {(detectedInfo.data as BarcodeResponse).brand && (
                <div style={{ color: '#595959' }}>
                  品牌: {(detectedInfo.data as BarcodeResponse).brand}
                </div>
              )}
              {(detectedInfo.data as BarcodeResponse).category && (
                <div style={{ color: '#595959' }}>
                  类别: {(detectedInfo.data as BarcodeResponse).category}
                </div>
              )}
              {(detectedInfo.data as BarcodeResponse).co2Factor && (
                <div style={{ color: '#595959', marginTop: 4 }}>
                  碳排放因子: {(detectedInfo.data as BarcodeResponse).co2Factor} {(detectedInfo.data as BarcodeResponse).unit || 'kg CO2e/kg'}
                </div>
              )}
            </div>
          </Card>
        )}

        {detectedInfo && detectedInfo.type === 'food' && (
          <Card
            size="small"
            style={{
              marginTop: 12,
              background: '#e6f7ff',
              border: '1px solid #91d5ff',
            }}
          >
            <div style={{ fontSize: 13 }}>
              <div style={{ fontWeight: 600, marginBottom: 4 }}>
                🍽️ 食物识别结果
              </div>
              <div style={{ color: '#595959' }}>
                识别为: {(detectedInfo.data as VisionResponse).label}
              </div>
              <div style={{ color: '#595959' }}>
                置信度: {((detectedInfo.data as VisionResponse).confidence * 100).toFixed(1)}%
              </div>
              <div style={{ color: '#8c8c8c', fontSize: 11, marginTop: 4 }}>
                请手动输入数量和碳排放因子
              </div>
            </div>
          </Card>
        )}

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
              <Form.Item 
                label="Emission factor (kg CO2e/kg)" 
                name="co2Factor"
              >
                <InputNumber
                  readOnly={detectedInfo?.type === 'barcode' && !!co2Factor}
                  style={{ width: '100%', ...inputBg }}
                  min={0}
                  step={0.1}
                  placeholder="自动识别或手动输入"
                />
              </Form.Item>
              <Form.Item label="Carbon Emissions (kg CO2e)">
                <Input
                  readOnly
                  value={emissions !== null ? emissions.toFixed(3) : ''}
                  style={inputBg}
                  placeholder="自动计算"
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

