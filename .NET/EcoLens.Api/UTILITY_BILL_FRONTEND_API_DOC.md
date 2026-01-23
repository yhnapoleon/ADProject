# 水电账单功能 - 前端对接文档

## 📋 概述

本文档说明前端如何调用水电账单相关的 API 接口，包括账单上传（OCR识别）、手动创建、查询、统计等功能。

**后端 API 地址：** `http://localhost:5133` （开发环境，根据实际情况修改）

**Swagger 文档地址：** `http://localhost:5133/swagger` （启动项目后访问，可查看详细 API 文档并在线测试）

**认证方式：** 所有接口需要在请求头中携带 JWT Token
```
Authorization: Bearer {your_jwt_token}
```

---

## 📦 文档获取方式

### 从 GitHub 分支下载

本文档和相关文件可以从 GitHub 分支直接下载：

**GitHub 仓库地址：** `https://github.com/yhnapoleon/ADProject`  
**分支名称：** `dev/hu-xt`

**下载方式：**

1. **直接访问文件（推荐）**
   - 主要文档：https://github.com/yhnapoleon/ADProject/blob/dev/hu-xt/.NET/EcoLens.Api/UTILITY_BILL_FRONTEND_API_DOC.md
   - 在 GitHub 网页上点击 "Raw" 按钮即可查看原始内容，或直接复制

2. **克隆整个分支**
   ```bash
   git clone -b dev/hu-xt https://github.com/yhnapoleon/ADProject.git
   ```

3. **只下载特定文件**
   - 在 GitHub 网页上找到文件
   - 点击文件 → 点击 "Raw" → 另存为

---

## 🔑 API Key 说明

### 后端 API Key（不需要）

**前端不需要后端的 Google Cloud Vision API Key。**

**原因：**
- ✅ 所有 OCR 识别（Google Cloud Vision API）都在后端完成
- ✅ 前端只需要调用后端 API（`http://localhost:5133/api/UtilityBill`）
- ✅ 后端已经处理了所有 OCR 相关功能
- ✅ API Key 保存在后端服务器，前端无法访问

**前端只需要：**
- 调用后端 API 接口
- 上传账单文件（图片或PDF）
- 使用后端返回的数据（用量、碳排放等）

---

## 🚀 API 接口列表

### 1. 上传账单文件（OCR识别）

**接口：** `POST /api/UtilityBill/upload`

**功能：** 上传账单文件（图片或PDF），系统自动进行 OCR 识别、数据提取、碳排放计算并保存到数据库

**请求方式：** `multipart/form-data`

**请求参数：**
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| file | File | 是 | 账单文件（支持格式：JPG、PNG、GIF、BMP、WEBP、PDF，最大10MB） |

**请求示例（使用 fetch）：**
```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

fetch('http://localhost:5133/api/UtilityBill/upload', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('token')}`
    // 注意：不要设置 Content-Type，浏览器会自动设置 multipart/form-data
  },
  body: formData
})
.then(response => response.json())
.then(data => {
  console.log('上传成功:', data);
})
.catch(error => {
  console.error('上传失败:', error);
});
```

**请求示例（使用 axios）：**
```javascript
import axios from 'axios';

const formData = new FormData();
formData.append('file', fileInput.files[0]);

axios.post('http://localhost:5133/api/UtilityBill/upload', formData, {
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('token')}`,
    'Content-Type': 'multipart/form-data'
  }
})
.then(response => {
  console.log('上传成功:', response.data);
})
.catch(error => {
  console.error('上传失败:', error);
});
```

**响应示例（成功）：**
```json
{
  "id": 1,
  "billType": 3,
  "billTypeName": "综合账单",
  "billPeriodStart": "2025-11-05T00:00:00Z",
  "billPeriodEnd": "2025-12-07T00:00:00Z",
  "electricityUsage": 524.1,
  "waterUsage": 17.0,
  "gasUsage": null,
  "electricityCarbonEmission": 213.1827,
  "waterCarbonEmission": 0.0,
  "gasCarbonEmission": 0.0,
  "totalCarbonEmission": 213.1827,
  "inputMethod": 0,
  "inputMethodName": "自动识别",
  "ocrConfidence": 0.9173,
  "createdAt": "2025-01-23T10:30:00Z"
}
```

**响应字段说明：**
| 字段 | 类型 | 说明 |
|------|------|------|
| id | number | 账单ID |
| billType | number | 账单类型（0-3，见下方枚举值） |
| billTypeName | string | 账单类型名称（中文） |
| billPeriodStart | string | 账单周期开始日期（ISO 8601格式） |
| billPeriodEnd | string | 账单周期结束日期（ISO 8601格式） |
| electricityUsage | number \| null | 用电量（kWh） |
| waterUsage | number \| null | 用水量（m³） |
| gasUsage | number \| null | 用气量（kWh 或 m³） |
| electricityCarbonEmission | number | 电力碳排放（kg CO2） |
| waterCarbonEmission | number | 水碳排放（kg CO2） |
| gasCarbonEmission | number | 燃气碳排放（kg CO2） |
| totalCarbonEmission | number | 总碳排放（kg CO2） |
| inputMethod | number | 输入方式（0=自动识别，1=手动输入） |
| inputMethodName | string | 输入方式名称（中文） |
| ocrConfidence | number \| null | OCR识别置信度（0-1，仅自动识别时有值） |
| createdAt | string | 创建时间（ISO 8601格式） |

**错误响应（文件格式不支持）：**
```json
{
  "error": "不支持的文件格式。支持的格式：JPG、PNG、GIF、BMP、WEBP、PDF"
}
```

**错误响应（文件过大）：**
```json
{
  "error": "文件大小不能超过 10MB"
}
```

**错误响应（OCR识别失败）：**
```json
{
  "error": "无法提取账单周期，请确保图片清晰或使用手动输入"
}
```

---

### 2. 手动创建账单

**接口：** `POST /api/UtilityBill/manual`

**功能：** 手动输入账单数据，系统自动计算碳排放并保存到数据库

**请求体：**
```json
{
  "billType": 0,
  "billPeriodStart": "2025-01-01T00:00:00Z",
  "billPeriodEnd": "2025-01-31T23:59:59Z",
  "electricityUsage": 150.5,
  "waterUsage": 10.2,
  "gasUsage": 25.3
}
```

**请求参数说明：**
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| billType | number | 是 | 账单类型（0-3，见下方枚举值） |
| billPeriodStart | string | 是 | 账单周期开始日期（ISO 8601格式） |
| billPeriodEnd | string | 是 | 账单周期结束日期（ISO 8601格式） |
| electricityUsage | number \| null | 否 | 用电量（kWh），根据账单类型填写 |
| waterUsage | number \| null | 否 | 用水量（m³），根据账单类型填写 |
| gasUsage | number \| null | 否 | 用气量（kWh 或 m³），根据账单类型填写 |

**响应示例（成功）：**
```json
{
  "id": 2,
  "billType": 0,
  "billTypeName": "电费",
  "billPeriodStart": "2025-01-01T00:00:00Z",
  "billPeriodEnd": "2025-01-31T23:59:59Z",
  "electricityUsage": 150.5,
  "waterUsage": null,
  "gasUsage": null,
  "electricityCarbonEmission": 69.98685,
  "waterCarbonEmission": 0.0,
  "gasCarbonEmission": 0.0,
  "totalCarbonEmission": 69.98685,
  "inputMethod": 1,
  "inputMethodName": "手动输入",
  "ocrConfidence": null,
  "createdAt": "2025-01-23T10:35:00Z"
}
```

**错误响应（参数验证失败）：**
```json
{
  "error": "Request validation failed",
  "errors": {
    "billType": ["账单类型不能为空"],
    "billPeriodStart": ["账单周期开始日期不能为空"],
    "billPeriodEnd": ["账单周期结束日期不能为空"]
  }
}
```

---

### 3. 获取当前用户的账单列表

**接口：** `GET /api/UtilityBill/my-bills`

**功能：** 获取当前登录用户的所有账单记录，支持筛选和分页

**查询参数：**
| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| startDate | string | 否 | 开始日期（格式：yyyy-MM-dd），基于账单周期结束日期筛选 |
| endDate | string | 否 | 结束日期（格式：yyyy-MM-dd），基于账单周期开始日期筛选 |
| billType | number | 否 | 账单类型筛选（0-3） |
| page | number | 否 | 页码，默认1，从1开始 |
| pageSize | number | 否 | 每页数量，默认20，最大100 |

**请求示例：**
```
GET /api/UtilityBill/my-bills
GET /api/UtilityBill/my-bills?startDate=2025-01-01&endDate=2025-01-31&billType=0&page=1&pageSize=20
```

**响应示例（成功）：**
```json
{
  "items": [
    {
      "id": 1,
      "billType": 3,
      "billTypeName": "综合账单",
      "billPeriodStart": "2025-11-05T00:00:00Z",
      "billPeriodEnd": "2025-12-07T00:00:00Z",
      "electricityUsage": 524.1,
      "waterUsage": 17.0,
      "totalCarbonEmission": 213.1827,
      "inputMethodName": "自动识别",
      "ocrConfidence": 0.9173,
      "createdAt": "2025-01-23T10:30:00Z"
    },
    {
      "id": 2,
      "billType": 0,
      "billTypeName": "电费",
      "billPeriodStart": "2025-01-01T00:00:00Z",
      "billPeriodEnd": "2025-01-31T23:59:59Z",
      "electricityUsage": 150.5,
      "totalCarbonEmission": 69.98685,
      "inputMethodName": "手动输入",
      "createdAt": "2025-01-23T10:35:00Z"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

**响应字段说明：**
| 字段 | 类型 | 说明 |
|------|------|------|
| items | array | 账单列表 |
| totalCount | number | 总记录数 |
| page | number | 当前页码 |
| pageSize | number | 每页数量 |
| totalPages | number | 总页数 |

---

### 4. 获取单条账单详情

**接口：** `GET /api/UtilityBill/{id}`

**功能：** 根据ID获取单条账单的详细信息

**路径参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| id | number | 账单ID |

**响应示例（成功）：**
```json
{
  "id": 1,
  "billType": 3,
  "billTypeName": "综合账单",
  "billPeriodStart": "2025-11-05T00:00:00Z",
  "billPeriodEnd": "2025-12-07T00:00:00Z",
  "electricityUsage": 524.1,
  "waterUsage": 17.0,
  "gasUsage": null,
  "electricityCarbonEmission": 213.1827,
  "waterCarbonEmission": 0.0,
  "gasCarbonEmission": 0.0,
  "totalCarbonEmission": 213.1827,
  "inputMethod": 0,
  "inputMethodName": "自动识别",
  "ocrConfidence": 0.9173,
  "createdAt": "2025-01-23T10:30:00Z"
}
```

**错误响应（记录不存在）：**
```json
{
  "error": "Bill not found or access denied"
}
```

---

### 5. 删除账单

**接口：** `DELETE /api/UtilityBill/{id}`

**功能：** 删除指定的账单记录

**路径参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| id | number | 账单ID |

**响应示例（成功）：**
```json
{
  "message": "Deleted successfully"
}
```

**错误响应（记录不存在）：**
```json
{
  "error": "Bill not found or access denied"
}
```

---

### 6. 获取账单统计信息

**接口：** `GET /api/UtilityBill/statistics`

**功能：** 获取当前用户的账单统计信息，包括总记录数、总用量、总碳排放等

**查询参数：**
| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| startDate | string | 否 | 开始日期（格式：yyyy-MM-dd） |
| endDate | string | 否 | 结束日期（格式：yyyy-MM-dd） |

**请求示例：**
```
GET /api/UtilityBill/statistics
GET /api/UtilityBill/statistics?startDate=2025-01-01&endDate=2025-01-31
```

**响应示例（成功）：**
```json
{
  "totalRecords": 2,
  "totalElectricityUsage": 674.6,
  "totalWaterUsage": 17.0,
  "totalGasUsage": 0.0,
  "totalCarbonEmission": 283.16955,
  "byBillType": [
    {
      "billType": 0,
      "billTypeName": "电费",
      "recordCount": 1,
      "totalElectricityUsage": 150.5,
      "totalWaterUsage": 0.0,
      "totalGasUsage": 0.0,
      "totalCarbonEmission": 69.98685
    },
    {
      "billType": 3,
      "billTypeName": "综合账单",
      "recordCount": 1,
      "totalElectricityUsage": 524.1,
      "totalWaterUsage": 17.0,
      "totalGasUsage": 0.0,
      "totalCarbonEmission": 213.1827
    }
  ]
}
```

**响应字段说明：**
| 字段 | 类型 | 说明 |
|------|------|------|
| totalRecords | number | 总记录数 |
| totalElectricityUsage | number | 总用电量（kWh） |
| totalWaterUsage | number | 总用水量（m³） |
| totalGasUsage | number | 总用气量（kWh 或 m³） |
| totalCarbonEmission | number | 总碳排放（kg CO2） |
| byBillType | array | 按账单类型统计的信息 |

---

## 📊 账单类型枚举值（UtilityBillType）

前端需要显示账单类型选择器，使用以下枚举值：

| 值 | 名称 | 说明 |
|---|------|------|
| 0 | Electricity | 电费账单 |
| 1 | Water | 水费账单 |
| 2 | Gas | 燃气费账单 |
| 3 | Combined | 综合账单（包含多种类型） |

**前端建议：** 创建一个下拉菜单或按钮组，让用户选择账单类型。

---

## 📊 输入方式枚举值（InputMethod）

| 值 | 名称 | 说明 |
|---|------|------|
| 0 | Auto | 自动识别（OCR） |
| 1 | Manual | 手动输入 |

**前端显示：** 在账单详情中显示输入方式，自动识别的账单可以显示 OCR 置信度。

---

## 📝 文件上传说明

### 支持的文件格式

- **图片格式：** JPG、PNG、GIF、BMP、WEBP
- **文档格式：** PDF
- **文件大小限制：** 最大 10MB

### 上传流程

1. **用户选择文件**
   - 前端需要验证文件格式和大小
   - 显示文件预览（如果是图片）

2. **上传文件**
   - 使用 `multipart/form-data` 格式
   - 在请求头中携带 JWT Token

3. **处理响应**
   - 成功：显示账单详情和计算的碳排放
   - 失败：显示错误信息，提示用户重试或使用手动输入

### 前端文件验证示例

```javascript
function validateFile(file) {
  // 验证文件类型
  const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/bmp', 'image/webp', 'application/pdf'];
  if (!allowedTypes.includes(file.type)) {
    return { valid: false, error: '不支持的文件格式。支持的格式：JPG、PNG、GIF、BMP、WEBP、PDF' };
  }
  
  // 验证文件大小（10MB = 10 * 1024 * 1024 字节）
  const maxSize = 10 * 1024 * 1024;
  if (file.size > maxSize) {
    return { valid: false, error: '文件大小不能超过 10MB' };
  }
  
  return { valid: true };
}
```

---

## ⚠️ 错误处理

### HTTP 状态码

| 状态码 | 说明 | 处理方式 |
|--------|------|----------|
| 200 | 成功 | 正常处理响应数据 |
| 400 | 请求参数错误 | 显示验证错误信息 |
| 401 | 未授权 | 跳转到登录页面 |
| 404 | 资源不存在 | 显示"记录不存在" |
| 500 | 服务器错误 | 显示"服务器错误，请稍后重试" |

### 验证错误格式

当请求参数验证失败时，返回格式：

```json
{
  "error": "Request validation failed",
  "errors": {
    "billType": ["账单类型不能为空"],
    "billPeriodStart": ["账单周期开始日期不能为空"],
    "electricityUsage": ["用电量必须是非负值"]
  }
}
```

**前端处理：** 遍历 errors 对象，在对应输入框下方显示错误信息。

### OCR 识别失败处理

如果 OCR 识别失败（无法提取账单周期或用量），系统会返回错误信息：

```json
{
  "error": "无法提取账单周期，请确保图片清晰或使用手动输入"
}
```

**前端建议：**
- 显示友好的错误提示
- 提供"使用手动输入"的按钮，跳转到手动输入页面
- 允许用户重新上传

---

## 🔐 认证说明

所有接口都需要在请求头中携带 JWT Token：

```javascript
// 示例：使用 fetch
fetch('http://localhost:5133/api/UtilityBill/my-bills', {
  method: 'GET',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${localStorage.getItem('token')}`
  }
})
```

---

## 📝 前端需要实现的功能

### 1. 账单上传页面
- [ ] 文件选择器（支持拖拽上传）
- [ ] 文件预览（图片格式）
- [ ] 文件格式和大小验证
- [ ] 上传进度显示
- [ ] 上传成功：显示账单详情和碳排放
- [ ] 上传失败：显示错误信息，提供手动输入选项

### 2. 手动创建账单页面
- [ ] 账单类型选择器（下拉菜单或按钮组）
- [ ] 账单周期日期选择器（开始日期、结束日期）
- [ ] 用量输入框（根据账单类型显示对应的输入框）
  - 电费账单：只显示用电量输入框
  - 水费账单：只显示用水量输入框
  - 燃气费账单：只显示用气量输入框
  - 综合账单：显示所有用量输入框
- [ ] 提交按钮
- [ ] 错误提示显示

### 3. 账单列表页面
- [ ] 显示所有账单记录
- [ ] 每条记录显示：
  - 账单类型和账单周期
  - 用量信息（根据账单类型显示）
  - 总碳排放量
  - 输入方式（自动识别/手动输入）
  - OCR置信度（如果是自动识别）
  - 创建时间
- [ ] 筛选功能（按日期范围、账单类型）
- [ ] 分页功能
- [ ] 点击记录查看详情

### 4. 账单详情页面
- [ ] 显示完整账单信息
- [ ] 用量详情（用电量、用水量、用气量）
- [ ] 碳排放详情（电力、水、燃气、总计）
- [ ] 输入方式显示
- [ ] OCR置信度显示（如果是自动识别）
- [ ] 删除按钮

### 5. 统计页面
- [ ] 总体统计（总记录数、总用量、总碳排放）
- [ ] 按账单类型统计（图表或表格）
- [ ] 日期范围筛选
- [ ] 数据可视化（可选）

---

## 🧪 测试建议

### 测试用例

1. **文件上传测试**
   - 正常上传（JPG、PNG、PDF）
   - 不支持的文件格式（DOC、TXT等）
   - 文件过大（>10MB）
   - OCR识别成功
   - OCR识别失败（模糊图片、非账单图片）

2. **手动创建测试**
   - 正常创建（所有字段填写）
   - 必填字段为空（验证错误）
   - 日期范围错误（结束日期早于开始日期）
   - 用量为负数（验证错误）

3. **列表和详情测试**
   - 获取列表（无筛选）
   - 获取列表（按日期范围筛选）
   - 获取列表（按账单类型筛选）
   - 获取列表（分页）
   - 获取不存在的记录（404错误）

4. **统计测试**
   - 获取统计信息（无筛选）
   - 获取统计信息（按日期范围筛选）

5. **认证测试**
   - 未登录访问（401错误）
   - Token 过期

---

## 📞 联系方式

如有问题，请联系后端开发人员。

---

## 📅 更新日志

- 2025-01-23: 初始版本，包含所有账单相关 API 接口说明
