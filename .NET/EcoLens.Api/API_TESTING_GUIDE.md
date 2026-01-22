# API 测试指南

## 📋 概述

本文档说明如何测试出行记录相关的 API 接口，确保功能正常工作。

**测试目的：**
- 验证 API 是否正常工作
- 发现潜在问题（Google Maps API 调用、数据库连接等）
- 确保业务逻辑正确（地址转坐标、路线计算、碳排放计算等）

---

## 🚀 快速开始

### 步骤 1：启动项目

在项目目录 `.NET/EcoLens.Api/` 下运行：

```powershell
dotnet run
```

项目启动后，你会看到类似输出：
```
Now listening on: http://localhost:5133
Swagger UI available at: http://localhost:5133/swagger
```

### 步骤 2：访问 Swagger UI

在浏览器中打开：`http://localhost:5133/swagger`

你可以在这里：
- 查看所有 API 接口
- 查看详细的接口说明
- 直接测试 API（点击 "Try it out"）

---

## 🧪 测试流程

### 第一步：获取 JWT Token

#### 方法 1：使用 Swagger UI

1. 在 Swagger UI 中找到 `POST /api/auth/register`
2. 点击 "Try it out"
3. 输入测试数据：
   ```json
   {
     "username": "testuser",
     "email": "test@example.com",
     "password": "Test123!"
   }
   ```
4. 点击 "Execute"
5. 复制返回的 `token` 值

#### 方法 2：使用 HTTP 文件

1. 打开 `EcoLens.Api.http` 文件
2. 运行 "2. 用户登录（获取 Token）"
3. 复制返回的 `token` 值
4. 将 Token 粘贴到文件顶部的 `@Token` 变量中

---

### 第二步：测试出行记录 API

#### 测试 1：预览路线（不保存）

**目的：** 测试地址转坐标、路线计算、碳排放计算功能

**操作：**
1. 在 Swagger UI 中找到 `POST /api/travel/preview`
2. 点击右上角的 "Authorize" 按钮
3. 输入：`Bearer {你的token}`
4. 点击 "Try it out"
5. 输入测试数据：
   ```json
   {
     "originAddress": "北京市朝阳区",
     "destinationAddress": "北京市海淀区",
     "transportMode": 3
   }
   ```
6. 点击 "Execute"

**预期结果：**
- 状态码：200
- 返回数据包含：
  - `originLatitude` 和 `originLongitude`（出发地坐标）
  - `destinationLatitude` 和 `destinationLongitude`（目的地坐标）
  - `distanceKilometers`（距离，大于 0）
  - `estimatedCarbonEmission`（碳排放量，大于 0）
  - `routePolyline`（路线编码，不为空）

**如果失败：**
- 检查 Google Maps API Key 是否配置正确
- 检查地址是否有效
- 查看服务器日志

---

#### 测试 2：创建出行记录

**目的：** 测试完整的创建流程，包括数据库保存

**操作：**
1. 在 Swagger UI 中找到 `POST /api/travel`
2. 确保已授权（输入 Token）
3. 点击 "Try it out"
4. 输入测试数据：
   ```json
   {
     "originAddress": "北京市天安门广场",
     "destinationAddress": "北京市故宫博物院",
     "transportMode": 0,
     "notes": "步行游览"
   }
   ```
5. 点击 "Execute"

**预期结果：**
- 状态码：200
- 返回数据包含：
  - `id`（新创建的记录ID）
  - `createdAt`（创建时间）
  - 所有路线和碳排放信息

**验证：**
- 记录 `id` 值
- 用于后续测试（获取详情、删除）

---

#### 测试 3：获取出行记录列表

**目的：** 测试查询功能

**操作：**
1. 在 Swagger UI 中找到 `GET /api/travel/my-travels`
2. 确保已授权
3. 点击 "Try it out"
4. 点击 "Execute"

**预期结果：**
- 状态码：200
- 返回数组，包含刚才创建的记录
- 记录按创建时间倒序排列

---

#### 测试 4：获取单条记录详情

**目的：** 测试根据ID获取详情功能

**操作：**
1. 在 Swagger UI 中找到 `GET /api/travel/{id}`
2. 确保已授权
3. 点击 "Try it out"
4. 输入刚才创建的记录的 `id`
5. 点击 "Execute"

**预期结果：**
- 状态码：200
- 返回完整的记录详情
- 包含 `routePolyline`（可用于地图绘制）

---

#### 测试 5：删除出行记录

**目的：** 测试删除功能

**操作：**
1. 在 Swagger UI 中找到 `DELETE /api/travel/{id}`
2. 确保已授权
3. 点击 "Try it out"
4. 输入要删除的记录的 `id`
5. 点击 "Execute"

**预期结果：**
- 状态码：200
- 返回：`{ "message": "删除成功" }`

**验证：**
- 再次获取列表，确认记录已删除

---

### 第三步：测试错误场景

#### 测试 6：无效地址

**目的：** 测试错误处理

**操作：**
1. 使用无效地址创建记录：
   ```json
   {
     "originAddress": "不存在的地址123456",
     "destinationAddress": "另一个不存在的地址",
     "transportMode": 3
   }
   ```

**预期结果：**
- 状态码：400
- 返回错误信息：`无法解析出发地地址` 或 `无法解析目的地地址`

---

#### 测试 7：缺少必填字段

**目的：** 测试数据验证

**操作：**
1. 创建记录时只提供部分字段：
   ```json
   {
     "originAddress": "北京市朝阳区"
   }
   ```

**预期结果：**
- 状态码：400
- 返回验证错误信息

---

#### 测试 8：未授权访问

**目的：** 测试认证机制

**操作：**
1. 不提供 Token，直接调用 API

**预期结果：**
- 状态码：401
- 返回：`Unauthorized`

---

#### 测试 9：获取不存在的记录

**目的：** 测试 404 错误处理

**操作：**
1. 使用不存在的 ID（如 99999）获取详情

**预期结果：**
- 状态码：404
- 返回：`{ "message": "出行记录不存在" }`

---

## 📊 测试检查清单

### 功能测试

- [ ] 预览路线功能正常
- [ ] 创建出行记录功能正常
- [ ] 获取列表功能正常
- [ ] 获取详情功能正常
- [ ] 删除记录功能正常

### 数据验证

- [ ] 返回的坐标数据正确（纬度在 -90 到 90 之间，经度在 -180 到 180 之间）
- [ ] 距离计算正确（大于 0）
- [ ] 碳排放计算正确（根据出行方式不同而不同）
- [ ] 路线 polyline 不为空

### 错误处理

- [ ] 无效地址返回 400 错误
- [ ] 缺少必填字段返回 400 错误
- [ ] 未授权访问返回 401 错误
- [ ] 不存在的记录返回 404 错误

### 不同出行方式测试

- [ ] 步行（TransportMode = 0）- 碳排放应该为 0
- [ ] 自行车（TransportMode = 1）- 碳排放应该为 0
- [ ] 地铁（TransportMode = 3）- 碳排放应该大于 0
- [ ] 公交车（TransportMode = 4）- 碳排放应该大于 0
- [ ] 出租车（TransportMode = 5）- 碳排放应该较大

---

## 🔧 使用 HTTP 文件测试

### 方法：使用 Visual Studio 或 VS Code 的 HTTP 客户端

1. 打开 `EcoLens.Api.http` 文件
2. 先运行 "2. 用户登录（获取 Token）"
3. 复制返回的 Token
4. 将 Token 粘贴到文件顶部的 `@Token` 变量中
5. 依次运行其他测试请求

### 优势：
- 可以保存测试用例
- 可以快速重复测试
- 可以测试多个场景

---

## 🐛 常见问题排查

### 问题 1：Google Maps API 调用失败

**症状：** 返回 400 错误，提示"无法解析地址"

**排查步骤：**
1. 检查 `appsettings.Development.json` 中的 API Key 是否正确
2. 检查 Google Cloud Console 中 API 是否已启用
3. 检查 API Key 是否有使用限制
4. 测试 API Key 是否有效（在浏览器中测试）

### 问题 2：数据库连接失败

**症状：** 返回 500 错误

**排查步骤：**
1. 检查 SQL Server 服务是否运行
2. 检查连接字符串是否正确
3. 检查数据库是否存在

### 问题 3：Token 无效

**症状：** 返回 401 错误

**排查步骤：**
1. 确认 Token 是否正确复制（包含 "Bearer " 前缀）
2. 检查 Token 是否过期
3. 重新登录获取新 Token

---

## 📝 测试记录模板

### 测试日期：__________

### 测试环境：
- API 地址：`http://localhost:5133`
- 数据库：SQL Server Express
- Google Maps API：已配置

### 测试结果：

| 测试项 | 状态 | 备注 |
|--------|------|------|
| 预览路线 | ✅/❌ | |
| 创建记录（地铁） | ✅/❌ | |
| 创建记录（步行） | ✅/❌ | |
| 获取列表 | ✅/❌ | |
| 获取详情 | ✅/❌ | |
| 删除记录 | ✅/❌ | |
| 错误处理 | ✅/❌ | |

### 发现的问题：

1. 
2. 
3. 

---

## ✅ 测试完成标准

所有以下项目都通过，说明 API 功能正常：

1. ✅ 可以成功预览路线
2. ✅ 可以成功创建出行记录
3. ✅ 可以成功获取记录列表
4. ✅ 可以成功获取记录详情
5. ✅ 可以成功删除记录
6. ✅ 错误处理正常（400、401、404）
7. ✅ 不同出行方式的碳排放计算正确
8. ✅ 返回的数据格式正确

---

## 📞 需要帮助？

如果测试过程中遇到问题：
1. 查看服务器日志（控制台输出）
2. 检查 Google Maps API 配置
3. 检查数据库连接
4. 联系后端开发人员

---

## 📅 更新日志

- 2024-XX-XX: 初始版本
