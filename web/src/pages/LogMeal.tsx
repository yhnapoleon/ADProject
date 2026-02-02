import { Button, Card, Form, Input, InputNumber, message, Space, Typography, Upload, Spin } from 'antd';
import type { UploadProps } from 'antd';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { BrowserMultiFormatReader, DecodeHintType, BarcodeFormat, NotFoundException, ChecksumException, FormatException } from '@zxing/library';
import request from '../utils/request';

type BarcodeScanResult = { barcode: string | null; failureReason?: 'not_found' | 'decode_failed' };

const BARCODE_MAX_IMAGE_DIM = 1600;

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
  const [manualBarcode, setManualBarcode] = useState('');
  const [manualBarcodeLoading, setManualBarcodeLoading] = useState(false);

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

  /** 将图片按最长边缩放到 maxDim，大图更容易被 zxing 识别 */
  const resizeImageToMaxDim = (img: HTMLImageElement, maxDim: number): Promise<HTMLImageElement> => {
    const w = img.naturalWidth;
    const h = img.naturalHeight;
    if (w <= maxDim && h <= maxDim) return Promise.resolve(img);
    const scale = maxDim / Math.max(w, h);
    const cw = Math.max(1, Math.round(w * scale));
    const ch = Math.max(1, Math.round(h * scale));
    const canvas = document.createElement('canvas');
    canvas.width = cw;
    canvas.height = ch;
    const ctx = canvas.getContext('2d');
    if (!ctx) return Promise.resolve(img);
    ctx.drawImage(img, 0, 0, cw, ch);
    const dataUrl = canvas.toDataURL('image/png');
    return new Promise((resolve, reject) => {
      const scaled = new Image();
      scaled.onload = () => resolve(scaled);
      scaled.onerror = reject;
      scaled.src = dataUrl;
    });
  };

  /** 中心裁剪为原图的 factor 比例（如 0.85 即裁掉边缘约 7.5%），减少反光/杂物干扰 */
  const centerCropImage = (img: HTMLImageElement, factor: number): Promise<HTMLImageElement> => {
    const w = img.naturalWidth;
    const h = img.naturalHeight;
    const cw = Math.max(1, Math.round(w * factor));
    const ch = Math.max(1, Math.round(h * factor));
    const sx = (w - cw) / 2;
    const sy = (h - ch) / 2;
    const canvas = document.createElement('canvas');
    canvas.width = cw;
    canvas.height = ch;
    const ctx = canvas.getContext('2d');
    if (!ctx) return Promise.resolve(img);
    ctx.drawImage(img, sx, sy, cw, ch, 0, 0, cw, ch);
    const dataUrl = canvas.toDataURL('image/png');
    return new Promise((resolve, reject) => {
      const cropped = new Image();
      cropped.onload = () => resolve(cropped);
      cropped.onerror = reject;
      cropped.src = dataUrl;
    });
  };

  /** 灰度 + 对比度增强，缓解罐身曲面、反光导致 zxing 无法定位条形码 */
  const preprocessForBarcode = (img: HTMLImageElement): Promise<HTMLImageElement> => {
    const w = img.naturalWidth;
    const h = img.naturalHeight;
    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext('2d');
    if (!ctx) return Promise.resolve(img);
    ctx.drawImage(img, 0, 0);
    const data = ctx.getImageData(0, 0, w, h);
    const d = data.data;
    let min = 255;
    let max = 0;
    for (let i = 0; i < d.length; i += 4) {
      const g = Math.round(0.299 * d[i] + 0.587 * d[i + 1] + 0.114 * d[i + 2]);
      d[i] = d[i + 1] = d[i + 2] = g;
      if (g < min) min = g;
      if (g > max) max = g;
    }
    const span = Math.max(1, max - min);
    const contrast = 1.4;
    for (let i = 0; i < d.length; i += 4) {
      const v = ((d[i] - min) / span) * 255;
      const out = Math.round(128 + (v - 128) * contrast);
      const c = Math.max(0, Math.min(255, out));
      d[i] = d[i + 1] = d[i + 2] = c;
    }
    ctx.putImageData(data, 0, 0);
    const dataUrl = canvas.toDataURL('image/png');
    return new Promise((resolve, reject) => {
      const out = new Image();
      out.onload = () => resolve(out);
      out.onerror = reject;
      out.src = dataUrl;
    });
  };

  /** 将图片旋转指定角度（90/180/270），返回新 Image。竖向条形码需旋转后才能被 zxing 识别 */
  const rotateImage = (img: HTMLImageElement, degrees: 90 | 180 | 270): Promise<HTMLImageElement> => {
    const w = img.naturalWidth;
    const h = img.naturalHeight;
    const canvas = document.createElement('canvas');
    if (degrees === 180) {
      canvas.width = w;
      canvas.height = h;
    } else {
      canvas.width = h;
      canvas.height = w;
    }
    const ctx = canvas.getContext('2d');
    if (!ctx) return Promise.resolve(img);
    const rad = (degrees * Math.PI) / 180;
    const cw = canvas.width;
    const ch = canvas.height;
    ctx.save();
    ctx.translate(cw / 2, ch / 2);
    ctx.rotate(rad);
    ctx.drawImage(img, -w / 2, -h / 2, w, h);
    ctx.restore();
    const dataUrl = canvas.toDataURL('image/png');
    return new Promise((resolve, reject) => {
      const rotated = new Image();
      rotated.onload = () => resolve(rotated);
      rotated.onerror = reject;
      rotated.src = dataUrl;
    });
  };

  /** 根据 zxing 异常区分：未找到条形码 vs 找到但无法解析数字 */
  const getBarcodeFailureReason = (e: unknown): 'not_found' | 'decode_failed' | undefined => {
    if (e instanceof NotFoundException) return 'not_found';
    if (e instanceof ChecksumException || e instanceof FormatException) return 'decode_failed';
    return undefined;
  };

  // 尝试从图片中识别条形码（多策略：EAN/UPC 优先 → 原图 → 缩放 → 旋转 → 预处理/中心裁剪 → dataURL）
  const scanBarcode = async (file: File): Promise<BarcodeScanResult> => {
    let objectUrl: string | null = null;
    let lastReason: 'not_found' | 'decode_failed' | undefined;
    try {
      const hints = new Map<DecodeHintType, unknown>([
        [DecodeHintType.TRY_HARDER, true],
        [DecodeHintType.POSSIBLE_FORMATS, [BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.UPC_A, BarcodeFormat.UPC_E]],
      ]);
      const codeReader = new BrowserMultiFormatReader(hints);
      const img = await fileToImage(file);
      objectUrl = img.src;

      const tryDecodeFromElement = async (el: HTMLImageElement): Promise<string | null> => {
        try {
          const result = await codeReader.decodeFromImageElement(el);
          return result?.getText() ?? null;
        } catch (e) {
          const r = getBarcodeFailureReason(e);
          if (r) lastReason = r;
          return null;
        }
      };

      const tryDecodeFromUrl = async (url: string): Promise<string | null> => {
        try {
          const result = await codeReader.decodeFromImageUrl(url);
          return result?.getText() ?? null;
        } catch (e) {
          const r = getBarcodeFailureReason(e);
          if (r) lastReason = r;
          return null;
        }
      };

      // 1. 原图
      let text = await tryDecodeFromElement(img);
      if (text) return { barcode: text };

      // 2. 大图缩放
      let workImg: HTMLImageElement = img;
      if (img.naturalWidth > BARCODE_MAX_IMAGE_DIM || img.naturalHeight > BARCODE_MAX_IMAGE_DIM) {
        workImg = await resizeImageToMaxDim(img, BARCODE_MAX_IMAGE_DIM);
        text = await tryDecodeFromElement(workImg);
        if (text) return { barcode: text };
      }

      // 3. 旋转 90°、180°、270°（竖向/倾斜条形码）
      for (const deg of [90, 180, 270] as const) {
        const rotated = await rotateImage(workImg, deg);
        text = await tryDecodeFromElement(rotated);
        if (text) return { barcode: text };
      }

      // 4. 灰度+对比度增强（罐身曲面、反光时 zxing 常无法定位条码，预处理可改善）
      const preprocessed = await preprocessForBarcode(workImg);
      text = await tryDecodeFromElement(preprocessed);
      if (text) return { barcode: text };

      // 5. 中心裁剪后再试（减少边缘反光、杂物干扰）
      const cropped = await centerCropImage(workImg, 0.85);
      text = await tryDecodeFromElement(cropped);
      if (text) return { barcode: text };

      // 6. 预处理 + 中心裁剪
      const preprocessedCropped = await centerCropImage(preprocessed, 0.9);
      text = await tryDecodeFromElement(preprocessedCropped);
      if (text) return { barcode: text };

      // 7. 预处理后的旋转
      for (const deg of [90, 180, 270] as const) {
        const rot = await rotateImage(preprocessed, deg);
        text = await tryDecodeFromElement(rot);
        if (text) return { barcode: text };
      }

      // 8. data URL
      const dataUrl = await fileToBase64(file);
      text = await tryDecodeFromUrl(dataUrl);
      if (text) return { barcode: text };

      if (lastReason) console.log('Barcode scan failure reason:', lastReason === 'not_found' ? 'Barcode not detected' : 'Barcode detected but could not decode');
      return { barcode: null, failureReason: lastReason };
    } catch (error) {
      console.log('Barcode scan failed:', error);
      if (lastReason) console.log('Barcode scan failure reason:', lastReason === 'not_found' ? 'Barcode not detected' : 'Barcode detected but could not decode');
      return { barcode: null, failureReason: lastReason };
    } finally {
      if (objectUrl) URL.revokeObjectURL(objectUrl);
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
        message.warning('Barcode not found, will try food recognition');
      } else {
        message.error('Barcode lookup failed');
      }
      return null;
    }
  };

  /** 罐装/曲面条形码识别不到时，可手动输入数字后查询 */
  const handleManualBarcodeQuery = async () => {
    const raw = manualBarcode.replace(/\s+/g, '').replace(/-/g, '');
    const digits = raw.replace(/\D/g, '');
    if (digits.length < 8 || digits.length > 14) {
      message.warning('Please enter 8–14 digit barcode (e.g. EAN-13 is 13 digits)');
      return;
    }
    setManualBarcodeLoading(true);
    try {
      const info = await fetchBarcodeInfo(digits);
      if (info) {
        setDetectionType('barcode');
        setDetectedInfo({ type: 'barcode', data: info });
        form.setFieldsValue({
          foodName: info.productName || info.carbonReferenceLabel || '',
          co2Factor: info.co2Factor ?? undefined,
        });
        message.success(`Found: ${info.productName || info.carbonReferenceLabel || 'Unknown product'}`);
      } else {
        const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
        if (!token) {
          message.error('Session expired, please log in again');
          navigate('/login');
        } else {
          message.warning('No product found for this barcode');
        }
      }
    } finally {
      setManualBarcodeLoading(false);
    }
  };

  /** 根据食物名称从后端获取碳排放因子（用于食物识别后自动填充） */
  const fetchEmissionFactorByFoodName = async (foodName: string): Promise<{ co2Factor: number; factorUnit: string } | null> => {
    try {
      const response: any = await request.post('/food/calculate', {
        FoodName: foodName,
        Quantity: 1,
        Unit: 'kg',
      });
      if (response?.co2Factor != null) {
        return {
          co2Factor: Number(response.co2Factor),
          factorUnit: response.factorUnit || 'kg',
        };
      }
      return null;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null; // 未找到该食物的碳因子，保持不填充
      }
      console.error('Fetch emission factor by food name:', error);
      return null;
    }
  };

  // 调用食物识别API（AI 模型推理较慢，需较长超时）
  const recognizeFood = async (file: File): Promise<VisionResponse | null> => {
    try {
      const formData = new FormData();
      formData.append('file', file);
      // 食物识别需运行双模型推理，CPU 上约 15-60 秒
      const response: any = await request.post('/vision/analyze', formData, {
        timeout: 60000,
      });
      // request 拦截器已经返回了 response.data，所以直接使用
      return response as VisionResponse;
    } catch (error: any) {
      console.error('Food recognition error:', error);
      if (error.response?.status === 401) {
        return null;
      }
      const status = error.response?.status;
      const isTimeout = error.code === 'ECONNABORTED' || error.message?.includes('timeout');
      const msg = isTimeout
        ? 'Recognition timed out; AI is slow, please try again later'
        : status === 502
          ? 'Food recognition service not running. Start Vision service (port 8000) first'
          : status === 400
            ? 'Invalid image upload, please try again'
            : 'Food recognition failed';
      message.error(msg);
      return null;
    }
  };

  const beforeUpload: UploadProps['beforeUpload'] = async (file) => {
    // 清除之前的错误消息
    message.destroy('detecting');
    
    // 检查 token 是否存在（用于 API 调用）
    const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
    if (!token) {
      message.error('Session expired, please log in again');
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
      const scanResult = await scanBarcode(file as unknown as File);
      const barcode = scanResult.barcode;
      const barcodeFailureReason = scanResult.failureReason;
      
      if (barcode) {
        setDetectionType('barcode');
        message.loading({ content: 'Barcode detected, fetching product...', key: 'detecting', duration: 0 });
        
        const barcodeInfo = await fetchBarcodeInfo(barcode);
        if (barcodeInfo) {
          setDetectedInfo({ type: 'barcode', data: barcodeInfo });
          // 自动填充表单
          form.setFieldsValue({
            foodName: barcodeInfo.productName || barcodeInfo.carbonReferenceLabel || '',
            co2Factor: barcodeInfo.co2Factor || undefined,
          });
          message.success({ content: `Recognized: ${barcodeInfo.productName || barcodeInfo.carbonReferenceLabel}`, key: 'detecting' });
        } else {
          // 检查 token 是否被清除（表示 401 错误）
          const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
          if (!token) {
            // 401 错误，显示提示并跳转
            message.error({ content: 'Session expired, please log in again', key: 'detecting' });
            setLoading(false);
            setTimeout(() => {
              navigate('/login');
            }, 2000);
            return false;
          }
          
          // 条形码查询失败，尝试食物识别
          message.loading({ content: 'Barcode lookup failed, trying food recognition...', key: 'detecting' });
          const foodInfo = await recognizeFood(file as unknown as File);
          if (foodInfo) {
            setDetectionType('food');
            setDetectedInfo({ type: 'food', data: foodInfo });
            form.setFieldsValue({
              foodName: foodInfo.label,
            });
            const factorResult = await fetchEmissionFactorByFoodName(foodInfo.label);
            if (factorResult) {
              form.setFieldsValue({ co2Factor: factorResult.co2Factor });
              message.success({ content: `Recognized: ${foodInfo.label}, emission factor matched`, key: 'detecting' });
            } else {
              message.success({ content: `Recognized: ${foodInfo.label}`, key: 'detecting' });
            }
          } else {
            // 再次检查 token 是否被清除（表示 401 错误）
            const tokenAfterFood = localStorage.getItem('token') || localStorage.getItem('adminToken');
            if (!tokenAfterFood) {
              // 401 错误，显示提示并跳转
              message.error({ content: 'Session expired, please log in again', key: 'detecting' });
              setLoading(false);
              setTimeout(() => {
                navigate('/login');
              }, 2000);
              return false;
            }
            message.warning({ content: 'Could not recognize image, please enter manually', key: 'detecting' });
          }
        }
      } else {
        // 没有识别到条形码，尝试食物识别
        setDetectionType('food');
        const reasonHint = barcodeFailureReason === 'decode_failed'
          ? 'Barcode detected but could not decode. '
          : barcodeFailureReason === 'not_found'
            ? 'No barcode detected. '
            : '';
        message.loading({ content: `${reasonHint}Recognizing food...`, key: 'detecting', duration: 0 });
        
        const foodInfo = await recognizeFood(file as unknown as File);
        if (foodInfo) {
          setDetectedInfo({ type: 'food', data: foodInfo });
          form.setFieldsValue({
            foodName: foodInfo.label,
          });
          const factorResult = await fetchEmissionFactorByFoodName(foodInfo.label);
          if (factorResult) {
            form.setFieldsValue({ co2Factor: factorResult.co2Factor });
            message.success({ content: `Recognized: ${foodInfo.label}, emission factor matched`, key: 'detecting' });
          } else {
            message.success({ content: `Recognized: ${foodInfo.label}`, key: 'detecting' });
          }
        } else {
          // 检查 token 是否被清除（表示 401 错误）
          const token = localStorage.getItem('token') || localStorage.getItem('adminToken');
          if (!token) {
            // 401 错误，显示提示并跳转
            message.error({ content: 'Session expired, please log in again', key: 'detecting' });
            setLoading(false);
            setTimeout(() => {
              navigate('/login');
            }, 2000);
            return false;
          }
          const manualHint = barcodeFailureReason === 'decode_failed'
            ? 'Barcode detected but could not decode. Ensure barcode is clear, complete, and not reflective, or try capturing only the barcode. You can also enter the barcode number manually.'
            : barcodeFailureReason === 'not_found'
              ? 'No barcode detected; food recognition also failed. For cans/curved barcodes you can enter the barcode number manually or fill in food details.'
              : 'Could not recognize image, please enter manually.';
          message.warning({ content: manualHint, key: 'detecting', duration: 6 });
        }
      }
    } catch (error: any) {
      console.error('Detection error:', error);
      if (error.response?.status === 401) {
        message.error({ content: 'Session expired, please log in again', key: 'detecting' });
        setTimeout(() => {
          navigate('/login');
        }, 2000);
      } else {
        message.error({ content: 'Recognition error', key: 'detecting' });
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
                  Recognizing image...
                </div>
              </div>
            ) : !previewUrl ? (
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontWeight: 800, color: '#674fa3', fontSize: 18 }}>Add photo</div>
                <div style={{ color: '#8c8c8c', fontSize: 12, marginTop: 6 }}>
                  Tap to choose an image, or drag & drop here
                </div>
                <div style={{ color: '#8c8c8c', fontSize: 11, marginTop: 4 }}>
                  (Barcode & food recognition)
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
                    {detectionType === 'barcode' ? '📷 Barcode' : '🍽️ Food'}
                  </div>
                )}
              </div>
            )}
          </div>
        </Upload>

        {/* 罐装/曲面条形码识别不到时，可手动输入数字 */}
        <div
          style={{
            marginTop: 12,
            padding: '12px 14px',
            background: '#fffbe6',
            border: '1px solid #ffe58f',
            borderRadius: 10,
            fontSize: 13,
          }}
        >
          <div style={{ fontWeight: 600, marginBottom: 6, color: '#ad8b00' }}>
            Can&apos;t scan barcode? Enter barcode number manually
          </div>
          <Space.Compact style={{ width: '100%' }}>
            <Input
              value={manualBarcode}
              onChange={(e) => setManualBarcode(e.target.value)}
              placeholder="e.g. 8888200708696 (spaces allowed)"
              maxLength={20}
              style={{ flex: 1 }}
              onPressEnter={handleManualBarcodeQuery}
            />
            <Button
              type="primary"
              loading={manualBarcodeLoading}
              onClick={handleManualBarcodeQuery}
              style={{ background: '#faad14', borderColor: '#faad14' }}
            >
              Look up
            </Button>
          </Space.Compact>
        </div>

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
                📦 Product info
              </div>
              <div style={{ color: '#595959' }}>
                Product: {(detectedInfo.data as BarcodeResponse).productName || (detectedInfo.data as BarcodeResponse).carbonReferenceLabel}
              </div>
              {(detectedInfo.data as BarcodeResponse).brand && (
                <div style={{ color: '#595959' }}>
                  Brand: {(detectedInfo.data as BarcodeResponse).brand}
                </div>
              )}
              {(detectedInfo.data as BarcodeResponse).category && (
                <div style={{ color: '#595959' }}>
                  Category: {(detectedInfo.data as BarcodeResponse).category}
                </div>
              )}
              {(detectedInfo.data as BarcodeResponse).co2Factor && (
                <div style={{ color: '#595959', marginTop: 4 }}>
                  Emission factor: {(detectedInfo.data as BarcodeResponse).co2Factor} {(detectedInfo.data as BarcodeResponse).unit || 'kg CO2e/kg'}
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
                🍽️ Food recognition
              </div>
              <div style={{ color: '#595959' }}>
                Recognized: {(detectedInfo.data as VisionResponse).label}
              </div>
              <div style={{ color: '#595959' }}>
                Confidence: {((detectedInfo.data as VisionResponse).confidence * 100).toFixed(1)}%
              </div>
              {co2Factor != null && Number(co2Factor) > 0 && (
                <div style={{ color: '#595959' }}>
                  Emission factor: {Number(co2Factor)} kg CO2e/kg
                </div>
              )}
              {(!co2Factor || Number(co2Factor) <= 0) && (
                <div style={{ color: '#8c8c8c', fontSize: 11, marginTop: 4 }}>
                  No emission factor for this food; enter amount and factor manually
                </div>
              )}
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
                  readOnly={!!detectedInfo && !!co2Factor}
                  style={{ width: '100%', ...inputBg }}
                  min={0}
                  step={0.1}
                  placeholder="Auto or enter manually"
                />
              </Form.Item>
              <Form.Item label="Carbon Emissions (kg CO2e)">
                <Input
                  readOnly
                  value={emissions !== null ? emissions.toFixed(3) : ''}
                  style={inputBg}
                  placeholder="Auto-calculated"
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

