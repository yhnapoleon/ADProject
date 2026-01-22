# 出行记录功能 - 前端对接文档

## 📋 概述

本文档说明前端如何调用出行记录相关的 API 接口。

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
   - 主要文档：https://github.com/yhnapoleon/ADProject/blob/dev/hu-xt/.NET/EcoLens.Api/FRONTEND_API_DOC.md
   - 请求示例文件：https://github.com/yhnapoleon/ADProject/blob/dev/hu-xt/.NET/EcoLens.Api/EcoLens.Api.http
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

**前端不需要后端的 Google Maps API Key。**

**原因：**
- ✅ 所有 Google Maps API 调用（地理编码、路线计算等）都在后端完成
- ✅ 前端只需要调用后端 API（`http://localhost:5133/api/travel`）
- ✅ 后端已经处理了所有 Google Maps 相关功能
- ✅ API Key 保存在后端服务器，前端无法访问

**前端只需要：**
- 调用后端 API 接口
- 使用后端返回的数据（坐标、路线、距离等）

### 前端地图显示 Key（可能需要）

**如果前端需要在页面上显示地图，可能需要自己的 Google Maps JavaScript API Key。**

**情况说明：**

| 情况 | 是否需要 Key | 说明 |
|------|-------------|------|
| 使用 Google Maps JavaScript API 显示地图 | ✅ 需要 | 需要申请 Google Maps JavaScript API Key（仅用于前端地图显示） |
| 使用 Leaflet.js 等开源地图库 | ❌ 不需要 | 开源库不需要 API Key |
| 只显示文字信息，不显示地图 | ❌ 不需要 | 不需要任何 API Key |

**如何申请前端地图 Key（如果需要）：**
1. 访问 [Google Cloud Console](https://console.cloud.google.com/)
2. 创建项目或选择现有项目
3. 启用 "Maps JavaScript API"
4. 创建 API Key
5. 配置 API Key 限制（建议限制为特定域名，更安全）

**注意：**
- 前端地图 Key 和后端 API Key 是**分开的**，互不影响
- 前端地图 Key 只用于加载 Google Maps JavaScript 库
- 后端 API Key 用于后端调用 Google Maps API（地理编码、路线计算等）

---

## 🚀 API 接口列表

### 1. 预览路线和碳排放（可选功能）

**接口：** `POST /api/travel/preview`

**功能：** 预览路线信息、距离、时间和预估碳排放，不保存到数据库

**请求体：**
```json
{
  "originAddress": "北京市朝阳区",
  "destinationAddress": "北京市海淀区",
  "transportMode": 3
}
```

**请求参数说明：**
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| originAddress | string | 是 | 出发地地址 |
| destinationAddress | string | 是 | 目的地地址 |
| transportMode | number | 是 | 出行方式（见下方枚举值） |

**响应示例（成功）：**
```json
{
  "originAddress": "北京市朝阳区",
  "originLatitude": 39.9042,
  "originLongitude": 116.4074,
  "destinationAddress": "北京市海淀区",
  "destinationLatitude": 39.9080,
  "destinationLongitude": 116.3974,
  "transportMode": 3,
  "transportModeName": "地铁",
  "distanceMeters": 15500,
  "distanceKilometers": 15.50,
  "durationSeconds": 1800,
  "durationText": "30分钟",
  "estimatedCarbonEmission": 0.465,
  "routePolyline": "编码后的路线字符串"
}
```

**响应字段说明：**
| 字段 | 类型 | 说明 |
|------|------|------|
| originLatitude/originLongitude | number | 出发地坐标（用于地图标记） |
| destinationLatitude/destinationLongitude | number | 目的地坐标（用于地图标记） |
| distanceKilometers | number | 路线距离（公里） |
| durationText | string | 预计时间（格式化字符串） |
| estimatedCarbonEmission | number | 预估碳排放量（kg CO2） |
| routePolyline | string | 路线编码，用于在地图上绘制路线 |

---

### 2. 创建出行记录

**接口：** `POST /api/travel`

**功能：** 创建一条出行记录并保存到数据库

**请求体：**
```json
{
  "originAddress": "北京市朝阳区",
  "destinationAddress": "北京市海淀区",
  "transportMode": 3,
  "notes": "上班通勤"
}
```

**请求参数说明：**
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| originAddress | string | 是 | 出发地地址 |
| destinationAddress | string | 是 | 目的地地址 |
| transportMode | number | 是 | 出行方式（见下方枚举值） |
| notes | string | 否 | 备注（最大1000字符） |

**响应示例（成功）：**
```json
{
  "id": 1,
  "createdAt": "2024-01-15T10:30:00Z",
  "transportMode": 3,
  "transportModeName": "地铁",
  "originAddress": "北京市朝阳区",
  "originLatitude": 39.9042,
  "originLongitude": 116.4074,
  "destinationAddress": "北京市海淀区",
  "destinationLatitude": 39.9080,
  "destinationLongitude": 116.3974,
  "distanceMeters": 15500,
  "distanceKilometers": 15.50,
  "durationSeconds": 1800,
  "durationText": "30分钟",
  "carbonEmission": 0.465,
  "routePolyline": "编码后的路线字符串",
  "notes": "上班通勤"
}
```

**响应字段说明：**
| 字段 | 类型 | 说明 |
|------|------|------|
| id | number | 记录ID |
| createdAt | string | 创建时间（ISO 8601格式） |
| carbonEmission | number | 碳排放量（kg CO2） |
| routePolyline | string | 路线编码，用于在地图上绘制路线 |

**错误响应示例：**
```json
{
  "errors": {
    "originAddress": ["出发地地址不能为空"],
    "transportMode": ["出行方式不能为空"]
  }
}
```

---

### 3. 获取当前用户的出行记录列表

**接口：** `GET /api/travel/my-travels`

**功能：** 获取当前登录用户的所有出行记录

**请求参数：** 无

**响应示例（成功）：**
```json
[
  {
    "id": 1,
    "createdAt": "2024-01-15T10:30:00Z",
    "transportMode": 3,
    "transportModeName": "地铁",
    "originAddress": "北京市朝阳区",
    "destinationAddress": "北京市海淀区",
    "distanceKilometers": 15.50,
    "carbonEmission": 0.465,
    ...
  },
  {
    "id": 2,
    ...
  }
]
```

---

### 4. 获取单条出行记录详情

**接口：** `GET /api/travel/{id}`

**功能：** 根据ID获取单条出行记录的详细信息

**路径参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| id | number | 出行记录ID |

**响应示例（成功）：**
```json
{
  "id": 1,
  "createdAt": "2024-01-15T10:30:00Z",
  "transportMode": 3,
  "transportModeName": "地铁",
  "originAddress": "北京市朝阳区",
  "originLatitude": 39.9042,
  "originLongitude": 116.4074,
  "destinationAddress": "北京市海淀区",
  "destinationLatitude": 39.9080,
  "destinationLongitude": 116.3974,
  "distanceMeters": 15500,
  "distanceKilometers": 15.50,
  "durationSeconds": 1800,
  "durationText": "30分钟",
  "carbonEmission": 0.465,
  "routePolyline": "编码后的路线字符串",
  "notes": "上班通勤"
}
```

**错误响应（记录不存在）：**
```json
{
  "status": 404,
  "message": "出行记录不存在"
}
```

---

### 5. 删除出行记录

**接口：** `DELETE /api/travel/{id}`

**功能：** 删除指定的出行记录

**路径参数：**
| 参数 | 类型 | 说明 |
|------|------|------|
| id | number | 出行记录ID |

**响应示例（成功）：**
```json
{
  "message": "删除成功"
}
```

---

## 📊 出行方式枚举值（TransportMode）

前端需要显示出行方式选择器，使用以下枚举值：

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
| 8 | Train | 火车 |
| 9 | Plane | 飞机 |

**前端建议：** 创建一个下拉菜单或按钮组，让用户选择出行方式。

---

## 🗺️ 地图绘制说明

### 使用 routePolyline 绘制路线

后端返回的 `routePolyline` 是 Google Maps 编码的路线字符串，前端可以使用以下方式绘制：

**⚠️ 注意：** 如果使用 Google Maps JavaScript API，需要前端自己的 Google Maps JavaScript API Key（见上方"API Key 说明"）。

#### 方案 1：使用 Google Maps JavaScript API

**需要：** 前端自己的 Google Maps JavaScript API Key

```javascript
// 解码 polyline
const decodedPath = google.maps.geometry.encoding.decodePath(routePolyline);

// 在地图上绘制路线
const routePath = new google.maps.Polyline({
  path: decodedPath,
  geodesic: true,
  strokeColor: '#FF0000',
  strokeOpacity: 1.0,
  strokeWeight: 2
});

routePath.setMap(map);
```

#### 方案 2：使用 Leaflet.js（开源，免费）

**优点：** 不需要 API Key，完全免费

```javascript
// 需要安装 @mapbox/polyline 库
import polyline from '@mapbox/polyline';

// 解码 polyline
const decodedPath = polyline.decode(routePolyline);

// 转换为 Leaflet 格式（注意：需要交换 lat/lng）
const leafletPath = decodedPath.map(point => [point[0], point[1]]);

// 绘制路线
L.polyline(leafletPath, {
  color: 'red',
  weight: 3
}).addTo(map);
```

#### 方案 3：使用其他地图库

大多数地图库都支持 polyline 解码，请参考对应库的文档。

---

## 📍 地图标记说明

使用返回的坐标在地图上标记出发地和目的地：

```javascript
// 标记出发地
const originMarker = new google.maps.Marker({
  position: {
    lat: response.originLatitude,
    lng: response.originLongitude
  },
  map: map,
  label: '起点',
  title: response.originAddress
});

// 标记目的地
const destinationMarker = new google.maps.Marker({
  position: {
    lat: response.destinationLatitude,
    lng: response.destinationLongitude
  },
  map: map,
  label: '终点',
  title: response.destinationAddress
});
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
  "errors": {
    "originAddress": ["出发地地址不能为空"],
    "destinationAddress": ["目的地地址长度不能超过500个字符"]
  }
}
```

**前端处理：** 遍历 errors 对象，在对应输入框下方显示错误信息。

---

## 🔐 认证说明

所有接口都需要在请求头中携带 JWT Token：

```javascript
// 示例：使用 fetch
fetch('http://localhost:5133/api/travel', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${localStorage.getItem('token')}`
  },
  body: JSON.stringify({
    originAddress: '北京市朝阳区',
    destinationAddress: '北京市海淀区',
    transportMode: 3
  })
})
```

---

## 📝 前端需要实现的功能

### 1. 创建出行记录页面
- [ ] 出发地地址输入框（支持地址自动补全）
- [ ] 目的地地址输入框（支持地址自动补全）
- [ ] 出行方式选择器（下拉菜单或按钮组）
- [ ] 备注输入框（可选）
- [ ] 提交按钮
- [ ] 错误提示显示

### 2. 路线预览功能（可选）
- [ ] 预览按钮（在提交前预览）
- [ ] 显示预览结果：
  - 地图显示路线
  - 显示距离、时间
  - 显示预估碳排放
- [ ] 确认保存按钮

### 3. 出行记录列表页面
- [ ] 显示所有出行记录
- [ ] 每条记录显示：
  - 出发地和目的地
  - 出行方式
  - 距离
  - 碳排放量
  - 创建时间
- [ ] 点击记录查看详情

### 4. 出行记录详情页面
- [ ] 显示完整信息
- [ ] 地图显示路线（使用 routePolyline）
- [ ] 标记出发地和目的地
- [ ] 删除按钮

### 5. 地图显示
- [ ] 集成地图库（Google Maps 或 Leaflet）
- [ ] 绘制路线（使用 routePolyline）
- [ ] 标记起点和终点
- [ ] 适配移动端和网页端

---

## 🧪 测试建议

### 测试用例

1. **创建记录测试**
   - 正常创建（所有字段填写）
   - 必填字段为空（验证错误）
   - 地址不存在（Google Maps 无法解析）

2. **预览功能测试**
   - 正常预览
   - 地址解析失败

3. **列表和详情测试**
   - 获取列表
   - 获取不存在的记录（404错误）

4. **认证测试**
   - 未登录访问（401错误）
   - Token 过期

---

## 📞 联系方式

如有问题，请联系后端开发人员。

---

## 📅 更新日志

- 2026-01-22: 添加文档获取方式和 API Key 说明
- 2024-XX-XX: 初始版本
