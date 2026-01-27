# EcoLens API - 前端对接完整文档

## 📋 概述

本文档是 EcoLens API 的完整前端对接说明，包含所有功能模块的 API 接口、请求参数、响应格式和使用示例。

**后端 API 地址：** `http://localhost:5133` （开发环境，根据实际情况修改）

**Swagger 文档地址：** `http://localhost:5133/swagger` （启动项目后访问，可查看详细 API 文档并在线测试）

**认证方式：** 所有接口（除注册/登录外）需要在请求头中携带 JWT Token
```
Authorization: Bearer {your_jwt_token}
```

---

## 📦 文档获取方式

### 从 GitHub 分支下载

**GitHub 仓库地址：** `https://github.com/yhnapoleon/ADProject`  
**分支名称：** `dev/hu-xt`

**下载方式：**
1. **直接访问文件（推荐）**
   - 主要文档：https://github.com/yhnapoleon/ADProject/blob/dev/hu-xt/.NET/EcoLens.Api/FRONTEND_API_INTEGRATION.md
   - 在 GitHub 网页上点击 "Raw" 按钮即可查看原始内容

2. **克隆整个分支**
   ```bash
   git clone -b dev/hu-xt https://github.com/yhnapoleon/ADProject.git
   ```

---

## 🔑 API Key 说明

### 前端不需要后端 API Key

**重要说明：**
- ✅ 所有第三方 API 调用（Google Maps、Google Vision、Climatiq 等）都在后端完成
- ✅ 前端只需要调用后端 API（`http://localhost:5133/api/*`）
- ✅ 后端已经处理了所有第三方 API 相关功能
- ✅ API Key 保存在后端服务器，前端无法访问

**前端只需要：**
- 调用后端 API 接口
- 上传文件（账单、活动图片等）
- 使用后端返回的数据

### 前端地图显示 Key（可选）

**如果前端需要在页面上显示地图，可能需要自己的 Google Maps JavaScript API Key。**

| 情况 | 是否需要 Key | 说明 |
|------|-------------|------|
| 使用 Google Maps JavaScript API 显示地图 | ✅ 需要 | 需要申请 Google Maps JavaScript API Key（仅用于前端地图显示） |
| 使用 Leaflet.js 等开源地图库 | ❌ 不需要 | 开源库不需要 API Key |
| 只显示文字信息，不显示地图 | ❌ 不需要 | 不需要任何 API Key |

**注意：** 前端地图 Key 和后端 API Key 是**分开的**，互不影响。

---

## 📚 目录

1. [认证模块 (Auth)](#1-认证模块-auth)
2. [用户资料模块 (UserProfile)](#2-用户资料模块-userprofile)
3. [出行记录模块 (Travel)](#3-出行记录模块-travel)
4. [水电账单模块 (UtilityBill)](#4-水电账单模块-utilitybill)
5. [活动记录模块 (Activity)](#5-活动记录模块-activity)
6. [社区模块 (Community)](#6-社区模块-community)
7. [排行榜模块 (Leaderboard)](#7-排行榜模块-leaderboard)
8. [条形码模块 (Barcode)](#8-条形码模块-barcode)
9. [AI 聊天模块 (AiChat)](#9-ai-聊天模块-aichat)
10. [洞察模块 (Insight)](#10-洞察模块-insight)
11. [碳排放因子模块 (CarbonFactor)](#11-碳排放因子模块-carbonfactor)
12. [错误处理](#错误处理)
13. [前端实现建议](#前端实现建议)

---

## 1. 认证模块 (Auth)

### 1.1 用户注册

**接口：** `POST /api/auth/register`

**功能：** 用户注册并返回认证 Token

**请求体：**
```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "Test123!"
}
```

**响应示例：**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "testuser",
    "email": "test@example.com",
    "role": "User",
    "avatarUrl": null,
    "totalCarbonSaved": 0,
    "currentPoints": 0
  }
}
```

### 1.2 用户登录

**接口：** `POST /api/auth/login`

**功能：** 用户登录并返回认证 Token

**请求体：**
```json
{
  "email": "test@example.com",
  "password": "Test123!"
}
```

**响应格式：** 同注册接口

---

## 2. 用户资料模块 (UserProfile)

### 2.1 获取用户资料

**接口：** `GET /api/user/profile`

**功能：** 获取当前登录用户的资料（包含排名）

**响应示例：**
```json
{
  "id": 1,
  "username": "testuser",
  "email": "test@example.com",
  "avatarUrl": null,
  "region": "Singapore",
  "totalCarbonSaved": 500.5,
  "currentPoints": 1000,
  "rank": 5,
  "role": "User"
}
```

### 2.2 更新用户资料

**接口：** `PUT /api/user/profile`

**功能：** 更新当前登录用户的头像、用户名与地区

**请求体：**
```json
{
  "nickname": "新用户名",
  "avatarUrl": "https://example.com/avatar.jpg",
  "region": "Singapore"
}
```

### 2.3 修改密码

**接口：** `POST /api/user/change-password`

**功能：** 修改密码（验证旧密码后更新为新密码）

**请求体：**
```json
{
  "oldPassword": "OldPass123!",
  "newPassword": "NewPass123!"
}
```

---

## 3. 出行记录模块 (Travel)

### 3.1 预览路线和碳排放

**接口：** `POST /api/travel/preview`

**功能：** 预览路线信息、距离、时间和预估碳排放，不保存到数据库

**请求体：**
```json
{
  "originAddress": "160149",
  "destinationAddress": "018956",
  "transportMode": 6
}
```

**说明：**
- 支持新加坡邮编直接输入（6位数字，如 `160149`）
- 系统会自动添加 "Singapore" 后缀以提高识别率

**响应示例：**
```json
{
  "originAddress": "160149 Singapore",
  "originLatitude": 1.2966,
  "originLongitude": 103.7764,
  "destinationAddress": "018956 Singapore",
  "destinationLatitude": 1.2966,
  "destinationLongitude": 103.7764,
  "transportMode": 6,
  "transportModeName": "私家车（汽油）",
  "distanceMeters": 15500,
  "distanceKilometers": 15.50,
  "durationSeconds": 1800,
  "durationText": "30分钟",
  "estimatedCarbonEmission": 3.255,
  "routePolyline": "编码后的路线字符串"
}
```

### 3.2 创建出行记录

**接口：** `POST /api/travel`

**功能：** 创建一条出行记录并保存到数据库

**请求体：**
```json
{
  "originAddress": "160149",
  "destinationAddress": "018956",
  "transportMode": 6,
  "notes": "上班通勤"
}
```

**响应示例：**
```json
{
  "id": 1,
  "createdAt": "2025-01-23T10:30:00Z",
  "transportMode": 6,
  "transportModeName": "私家车（汽油）",
  "originAddress": "160149 Singapore",
  "originLatitude": 1.2966,
  "originLongitude": 103.7764,
  "destinationAddress": "018956 Singapore",
  "destinationLatitude": 1.2966,
  "destinationLongitude": 103.7764,
  "distanceMeters": 15500,
  "distanceKilometers": 15.50,
  "durationSeconds": 1800,
  "durationText": "30分钟",
  "carbonEmission": 3.255,
  "routePolyline": "编码后的路线字符串",
  "notes": "上班通勤"
}
```

### 3.3 获取出行记录列表

**接口：** `GET /api/travel/my-travels`

**功能：** 获取当前登录用户的所有出行记录，支持筛选和分页

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| startDate | string | 开始日期（格式：yyyy-MM-dd） |
| endDate | string | 结束日期（格式：yyyy-MM-dd） |
| transportMode | number | 出行方式筛选（0-9） |
| page | number | 页码，默认1 |
| pageSize | number | 每页数量，默认20，最大100 |

**响应示例：**
```json
{
  "items": [...],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

### 3.4 获取出行记录详情

**接口：** `GET /api/travel/{id}`

**功能：** 根据ID获取单条出行记录的详细信息

### 3.5 删除出行记录

**接口：** `DELETE /api/travel/{id}`

**功能：** 删除指定的出行记录

### 3.6 获取出行统计

**接口：** `GET /api/travel/statistics`

**功能：** 获取当前用户的出行统计信息

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| startDate | string | 开始日期（格式：yyyy-MM-dd） |
| endDate | string | 结束日期（格式：yyyy-MM-dd） |

**响应示例：**
```json
{
  "totalRecords": 10,
  "totalDistanceKilometers": 150.5,
  "totalCarbonEmission": 30.5,
  "byTransportMode": [
    {
      "transportMode": 6,
      "transportModeName": "私家车（汽油）",
      "recordCount": 5,
      "totalDistanceKilometers": 100.0,
      "totalCarbonEmission": 21.0
    }
  ]
}
```

### 📊 出行方式枚举值（TransportMode）

| 值 | 名称 | 说明 |
|---|------|------|
| 0 | Walking | 步行 |
| 1 | Bicycle | 自行车 |
| 2 | ElectricBike | 电动车 |
| 3 | Subway | 地铁 |
| 4 | Bus | 公交车 |
| 5 | Taxi | 出租车/网约车 |
| 6 | CarGasoline | 私家车（汽油） |
| 7 | CarElectric | 私家车（电动车） |
| 8 | Ship | 轮船 |
| 9 | Plane | 飞机 |

---

## 4. 水电账单模块 (UtilityBill)

### 4.1 上传账单文件（OCR识别）

**接口：** `POST /api/UtilityBill/upload`

**功能：** 上传账单文件（图片或PDF），系统自动进行 OCR 识别、数据提取、碳排放计算并保存到数据库

**请求方式：** `multipart/form-data`

**请求参数：**
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| file | File | 是 | 账单文件（支持格式：JPG、PNG、BMP、WEBP、PDF，最大10MB） |

**请求示例：**
```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

fetch('http://localhost:5133/api/UtilityBill/upload', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('token')}`
  },
  body: formData
})
.then(response => response.json())
.then(data => console.log('上传成功:', data))
.catch(error => console.error('上传失败:', error));
```

**响应示例：**
```json
{
  "id": 1,
  "billType": 3,
  "billTypeName": "综合账单",
  "billPeriodStart": "2025-11-04T00:00:00Z",
  "billPeriodEnd": "2025-12-02T00:00:00Z",
  "electricityUsage": 468.0,
  "waterUsage": 7.6,
  "gasUsage": null,
  "electricityCarbonEmission": 213.18,
  "waterCarbonEmission": 0.0,
  "gasCarbonEmission": 0.0,
  "totalCarbonEmission": 213.18,
  "inputMethod": 0,
  "inputMethodName": "自动识别",
  "ocrConfidence": 0.9173,
  "createdAt": "2025-01-23T10:30:00Z"
}
```

**重要提示：**
- 如果检测到重复账单（相同用户、相同账单周期、相同类型、相同用量），会返回错误提示，不会保存
- 错误信息：`"检测到重复账单。该账单已存在（账单ID: 123，账单周期: 2025-11-04 至 2025-12-02）。请勿重复上传相同账单。"`

### 4.2 手动创建账单

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

**重要提示：**
- 如果检测到重复账单，会返回错误提示，不会保存

### 4.3 获取账单列表

**接口：** `GET /api/UtilityBill/my-bills`

**功能：** 获取当前登录用户的所有账单记录，支持筛选和分页

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| startDate | string | 开始日期（格式：yyyy-MM-dd） |
| endDate | string | 结束日期（格式：yyyy-MM-dd） |
| billType | number | 账单类型筛选（0-3） |
| page | number | 页码，默认1 |
| pageSize | number | 每页数量，默认20，最大100 |

### 4.4 获取账单详情

**接口：** `GET /api/UtilityBill/{id}`

**功能：** 根据ID获取单条账单的详细信息

### 4.5 删除账单

**接口：** `DELETE /api/UtilityBill/{id}`

**功能：** 删除指定的账单记录

### 4.6 获取账单统计

**接口：** `GET /api/UtilityBill/statistics`

**功能：** 获取当前用户的账单统计信息

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| startDate | string | 开始日期（格式：yyyy-MM-dd） |
| endDate | string | 结束日期（格式：yyyy-MM-dd） |

**响应示例：**
```json
{
  "totalRecords": 2,
  "totalElectricityUsage": 674.6,
  "totalWaterUsage": 17.0,
  "totalGasUsage": 0.0,
  "totalCarbonEmission": 283.17,
  "byBillType": [...]
}
```

### 📊 账单类型枚举值（UtilityBillType）

| 值 | 名称 | 说明 |
|---|------|------|
| 0 | Electricity | 电费账单 |
| 1 | Water | 水费账单 |
| 2 | Gas | 燃气费账单 |
| 3 | Combined | 综合账单（包含多种类型） |

### 📊 输入方式枚举值（InputMethod）

| 值 | 名称 | 说明 |
|---|------|------|
| 0 | Auto | 自动识别（OCR） |
| 1 | Manual | 手动输入 |

---

## 5. 活动记录模块 (Activity)

### 5.1 上传活动记录

**接口：** `POST /api/activity/upload`

**功能：** 上传活动记录（图片或手动输入），根据标签查找参考，计算排放并保存

**请求方式：** `multipart/form-data`

**请求参数：**
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| label | string | 是 | 活动标签（如：Beef, Chicken, Electricity等） |
| category | number | 是 | 活动类别（0=Food, 1=Transport, 2=Utility, 3=Other） |
| quantity | number | 是 | 数量 |
| unit | string | 是 | 单位（如：kg, km, kWh等） |
| image | File | 否 | 活动图片（可选） |
| region | string | 否 | 地区（用于Utility类别匹配） |

**响应示例：**
```json
{
  "id": 1,
  "label": "Beef",
  "category": 0,
  "quantity": 1.5,
  "unit": "kg",
  "carbonEmission": 45.0,
  "createdAt": "2025-01-23T10:30:00Z"
}
```

---

## 6. 社区模块 (Community)

### 6.1 获取帖子列表

**接口：** `GET /api/community`

**功能：** 获取社区帖子列表，支持筛选和分页

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| type | number | 帖子类型筛选 |
| page | number | 页码，默认1 |
| pageSize | number | 每页数量，默认20 |

### 6.2 创建帖子

**接口：** `POST /api/community`

**功能：** 创建新的社区帖子

**请求体：**
```json
{
  "title": "我的环保心得",
  "content": "分享一些环保小贴士...",
  "imageUrls": "https://example.com/image1.jpg,https://example.com/image2.jpg",
  "type": 0
}
```

### 6.3 获取帖子详情

**接口：** `GET /api/community/{id}`

**功能：** 根据ID获取帖子详情

### 6.4 点赞/取消点赞

**接口：** `POST /api/community/{id}/like`

**功能：** 点赞或取消点赞帖子

### 6.5 评论帖子

**接口：** `POST /api/community/{id}/comments`

**功能：** 对帖子进行评论

**请求体：**
```json
{
  "content": "很好的分享！"
}
```

---

## 7. 排行榜模块 (Leaderboard)

### 7.1 获取总排行榜

**接口：** `GET /api/leaderboard/top-users`

**功能：** 获取总减排量前 10 名用户

**响应示例：**
```json
[
  {
    "username": "user1",
    "avatarUrl": "https://example.com/avatar1.jpg",
    "totalCarbonSaved": 1000.5
  },
  {
    "username": "user2",
    "avatarUrl": null,
    "totalCarbonSaved": 950.2
  }
]
```

### 7.2 关注用户

**接口：** `POST /api/leaderboard/follow/{targetUserId}`

**功能：** 关注某个用户

### 7.3 获取关注列表排行榜

**接口：** `GET /api/leaderboard/friends`

**功能：** 获取我关注的人的排行榜数据（按总减排量排序）

---

## 8. 条形码模块 (Barcode)

### 8.1 获取条形码列表

**接口：** `GET /api/barcode`

**功能：** 获取条形码参考列表，支持搜索

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| barcode | string | 条形码搜索 |
| productName | string | 产品名称搜索 |
| category | string | 类别搜索 |
| brand | string | 品牌搜索 |

### 8.2 根据条形码获取产品信息

**接口：** `GET /api/barcode/{barcode}`

**功能：** 根据条形码获取产品信息和碳排放数据（优先本地；不存在时从 Open Food Facts 拉取并缓存）

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| useClimatiq | bool | 是否使用 Climatiq API 作为后备数据源，默认 false |

**响应示例：**
```json
{
  "barcode": "1234567890123",
  "productName": "Product Name",
  "category": "Food",
  "brand": "Brand Name",
  "carbonReference": {
    "labelName": "Product",
    "co2Factor": 2.5,
    "unit": "kgCO2/kg",
    "source": "OpenFoodFacts"
  }
}
```

---

## 9. AI 聊天模块 (AiChat)

### 9.1 AI 聊天

**接口：** `POST /api/ai/chat`

**功能：** 与 AI 助手聊天，获取环保建议

**请求体：**
```json
{
  "message": "如何减少碳排放？"
}
```

**响应示例：**
```json
{
  "reply": "减少碳排放的方法包括：1. 使用公共交通... 2. 节约用电..."
}
```

---

## 10. 洞察模块 (Insight)

### 10.1 获取周报洞察

**接口：** `GET /api/insight/weekly-report`

**功能：** 生成一条模拟周报洞见（最近 7 天）

**响应示例：**
```json
{
  "id": 0,
  "content": "Based on your eating habits over the last week, try substituting steak with plant-based options twice to reduce your emissions.",
  "type": 0,
  "isRead": false,
  "createdAt": "2025-01-23T10:30:00Z"
}
```

---

## 11. 碳排放因子模块 (CarbonFactor)

### 11.1 获取碳排放因子列表

**接口：** `GET /api/carbon/factors`

**功能：** 获取碳排放因子列表，支持按类别过滤

**查询参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| category | string | 类别过滤（Food, Transport, Utility, Other） |

**响应示例：**
```json
[
  {
    "id": 1,
    "labelName": "Beef",
    "category": 0,
    "co2Factor": 27.0,
    "unit": "kgCO2/kg"
  },
  {
    "id": 2,
    "labelName": "CarGasoline",
    "category": 1,
    "co2Factor": 0.21,
    "unit": "kgCO2/km"
  }
]
```

---

## 错误处理

### HTTP 状态码

| 状态码 | 说明 | 处理方式 |
|--------|------|----------|
| 200 | 成功 | 正常处理响应数据 |
| 400 | 请求参数错误 | 显示验证错误信息 |
| 401 | 未授权 | 跳转到登录页面，重新获取 Token |
| 404 | 资源不存在 | 显示"记录不存在" |
| 409 | 冲突 | 显示冲突信息（如：用户已存在、重复账单等） |
| 500 | 服务器错误 | 显示"服务器错误，请稍后重试" |

### 错误响应格式

**验证错误：**
```json
{
  "error": "Request validation failed",
  "errors": {
    "originAddress": ["出发地地址不能为空"],
    "transportMode": ["出行方式不能为空"]
  }
}
```

**业务错误：**
```json
{
  "error": "检测到重复账单。该账单已存在（账单ID: 123，账单周期: 2025-11-04 至 2025-12-02）。请勿重复上传相同账单。"
}
```

**前端处理建议：**
- 遍历 `errors` 对象，在对应输入框下方显示错误信息
- 对于业务错误，显示友好的错误提示
- 401 错误时，清除本地 Token，跳转到登录页面

---

## 前端实现建议

### 1. 认证管理

**Token 存储：**
```javascript
// 登录成功后保存 Token
localStorage.setItem('token', response.token);

// 请求时携带 Token
headers: {
  'Authorization': `Bearer ${localStorage.getItem('token')}`
}

// Token 过期处理
if (response.status === 401) {
  localStorage.removeItem('token');
  window.location.href = '/login';
}
```

### 2. 地址输入增强

**支持新加坡邮编：**
- 用户输入 6 位数字时，自动识别为新加坡邮编
- 后端会自动添加 "Singapore" 后缀
- 前端可以显示提示："支持直接输入新加坡邮编（6位数字）"

### 3. 文件上传

**文件验证：**
```javascript
function validateFile(file) {
  const allowedTypes = ['image/jpeg', 'image/png', 'image/bmp', 'image/webp', 'application/pdf'];
  if (!allowedTypes.includes(file.type)) {
    return { valid: false, error: '不支持的文件格式。支持的格式：JPG、PNG、BMP、WEBP、PDF' };
  }
  
  const maxSize = 10 * 1024 * 1024; // 10MB
  if (file.size > maxSize) {
    return { valid: false, error: '文件大小不能超过 10MB' };
  }
  
  return { valid: true };
}
```

**上传进度：**
```javascript
const xhr = new XMLHttpRequest();
xhr.upload.addEventListener('progress', (e) => {
  if (e.lengthComputable) {
    const percentComplete = (e.loaded / e.total) * 100;
    updateProgressBar(percentComplete);
  }
});
```

### 4. 重复账单处理

**检测到重复账单时：**
- 显示友好的错误提示
- 提供查看已存在账单的链接
- 允许用户选择是否覆盖（如果需要）

### 5. 地图显示

**使用 routePolyline 绘制路线：**

**方案 1：Google Maps JavaScript API**
```javascript
const decodedPath = google.maps.geometry.encoding.decodePath(routePolyline);
const routePath = new google.maps.Polyline({
  path: decodedPath,
  geodesic: true,
  strokeColor: '#FF0000',
  strokeOpacity: 1.0,
  strokeWeight: 2
});
routePath.setMap(map);
```

**方案 2：Leaflet.js（开源，免费）**
```javascript
import polyline from '@mapbox/polyline';
const decodedPath = polyline.decode(routePolyline);
const leafletPath = decodedPath.map(point => [point[0], point[1]]);
L.polyline(leafletPath, { color: 'red', weight: 3 }).addTo(map);
```

### 6. 分页处理

**统一的分页响应格式：**
```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

**前端实现：**
- 显示当前页/总页数
- 提供上一页/下一页按钮
- 支持跳转到指定页

### 7. 日期格式

**统一使用 ISO 8601 格式：**
- 请求：`"2025-01-23T10:30:00Z"`
- 响应：`"2025-01-23T10:30:00Z"`
- 前端显示时转换为本地格式

---

## 📝 前端需要实现的功能清单

### 认证模块
- [ ] 注册页面
- [ ] 登录页面
- [ ] Token 管理
- [ ] 自动刷新 Token（如果需要）

### 用户资料模块
- [ ] 用户资料页面
- [ ] 编辑资料页面
- [ ] 修改密码页面
- [ ] 头像上传功能

### 出行记录模块
- [ ] 创建出行记录页面
  - [ ] 地址输入（支持新加坡邮编）
  - [ ] 出行方式选择器
  - [ ] 路线预览功能
- [ ] 出行记录列表页面
  - [ ] 筛选功能（日期、出行方式）
  - [ ] 分页功能
- [ ] 出行记录详情页面
  - [ ] 地图显示路线
  - [ ] 标记起点和终点
- [ ] 出行统计页面

### 水电账单模块
- [ ] 账单上传页面
  - [ ] 文件选择器（支持拖拽）
  - [ ] 文件预览
  - [ ] 上传进度显示
  - [ ] 重复账单提示
- [ ] 手动创建账单页面
  - [ ] 账单类型选择器
  - [ ] 日期选择器
  - [ ] 用量输入框
- [ ] 账单列表页面
  - [ ] 筛选功能
  - [ ] 分页功能
- [ ] 账单详情页面
- [ ] 账单统计页面

### 活动记录模块
- [ ] 活动上传页面
- [ ] 活动列表页面
- [ ] 活动统计页面

### 社区模块
- [ ] 帖子列表页面
- [ ] 创建帖子页面
- [ ] 帖子详情页面
- [ ] 点赞功能
- [ ] 评论功能

### 排行榜模块
- [ ] 总排行榜页面
- [ ] 关注列表排行榜
- [ ] 关注/取消关注功能

### 条形码模块
- [ ] 条形码扫描页面
- [ ] 产品信息显示
- [ ] 碳排放数据展示

### AI 聊天模块
- [ ] AI 聊天界面
- [ ] 消息历史记录

### 洞察模块
- [ ] 洞察列表页面
- [ ] 洞察详情页面

---

## 🧪 测试建议

### 1. 功能测试
- 所有 CRUD 操作（创建、读取、更新、删除）
- 文件上传（各种格式、大小）
- 筛选和分页功能
- 错误处理（网络错误、验证错误等）

### 2. 边界测试
- 空数据列表
- 大数据量分页
- 文件大小限制
- 日期范围边界

### 3. 兼容性测试
- 不同浏览器（Chrome、Firefox、Safari、Edge）
- 移动端和桌面端
- 不同屏幕尺寸

---

## 📞 联系方式

如有问题，请联系后端开发人员。

---

## 📅 更新日志

- 2025-01-26: 创建统一前端对接文档，整合所有模块
- 2025-01-26: 添加重复账单检测说明
- 2025-01-26: 添加新加坡邮编支持说明
- 2025-01-26: 更新出行方式（火车改为轮船）
