# Google Maps API 功能测试检查清单

## 📋 测试目标

确保今天构建的所有 Google Maps API 相关功能正常工作，包括：
- 地址转坐标（Geocoding API）
- 路线获取（Directions API）
- 缓存功能
- 碳排放计算
- 数据库操作
- 错误处理

---

## ✅ 测试步骤

### 1. 基础功能测试

#### 1.1 预览路线功能（测试 Geocoding + Directions API）

**测试步骤：**
1. 打开 Swagger UI: `http://localhost:5133/swagger`
2. 先注册/登录获取 Token
3. 测试 `POST /api/travel/preview`
4. 请求体：
   ```json
   {
     "originAddress": "Beijing Chaoyang District",
     "destinationAddress": "Beijing Haidian District",
     "transportMode": 3
   }
   ```

**预期结果：**
- ✅ 状态码：200
- ✅ 返回数据包含：
  - `originLatitude` 和 `originLongitude`（坐标不为空）
  - `destinationLatitude` 和 `destinationLongitude`（坐标不为空）
  - `distanceKilometers`（距离 > 0）
  - `estimatedCarbonEmission`（碳排放 > 0）
  - `routePolyline`（路线编码不为空，可用于地图绘制）

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

#### 1.2 创建出行记录（测试完整流程）

**测试步骤：**
1. 测试 `POST /api/travel`
2. 请求体（步行，碳排放应为0）：
   ```json
   {
     "originAddress": "Tiananmen Square, Beijing",
     "destinationAddress": "Forbidden City, Beijing",
     "transportMode": 0,
     "notes": "Walking test"
   }
   ```

**预期结果：**
- ✅ 状态码：200
- ✅ 返回数据包含：
  - `id`（新创建的记录ID）
  - `carbonEmission` = 0（步行应该为0）
  - `routePolyline`（不为空）

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

#### 1.3 创建出行记录（地铁，测试碳排放计算）

**测试步骤：**
1. 测试 `POST /api/travel`
2. 请求体：
   ```json
   {
     "originAddress": "Beijing Chaoyang District",
     "destinationAddress": "Beijing Haidian District",
     "transportMode": 3,
     "notes": "Subway test"
   }
   ```

**预期结果：**
- ✅ 状态码：200
- ✅ `carbonEmission` > 0（地铁应该有碳排放）

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

### 2. 缓存功能测试

#### 2.1 测试缓存是否生效

**测试步骤：**
1. 第一次调用预览API（记录响应时间）
2. 立即第二次调用相同的预览API（相同地址）
3. 比较两次响应时间

**预期结果：**
- ✅ 第二次调用应该更快（使用缓存）
- ✅ 两次返回的数据应该相同
- ✅ 查看服务器日志，应该看到缓存相关的日志

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

### 3. 数据库操作测试

#### 3.1 获取出行记录列表

**测试步骤：**
1. 测试 `GET /api/travel/my-travels`
2. 检查返回的数据格式

**预期结果：**
- ✅ 状态码：200
- ✅ 返回分页结果：
  - `items`（数组）
  - `totalCount`（总数）
  - `page`（当前页码）
  - `pageSize`（每页数量）
  - `totalPages`（总页数）

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

#### 3.2 获取单条记录详情

**测试步骤：**
1. 先创建一条记录，记录返回的 `id`
2. 测试 `GET /api/travel/{id}`
3. 使用刚才创建的记录ID

**预期结果：**
- ✅ 状态码：200
- ✅ 返回完整的记录信息
- ✅ 包含 `routePolyline`（可用于地图绘制）

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

#### 3.3 删除记录

**测试步骤：**
1. 先创建一条记录，记录返回的 `id`
2. 测试 `DELETE /api/travel/{id}`
3. 再次获取该记录，应该返回404

**预期结果：**
- ✅ 删除时状态码：200
- ✅ 再次获取时状态码：404

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

### 4. 统计功能测试

#### 4.1 获取统计信息

**测试步骤：**
1. 测试 `GET /api/travel/statistics`
2. 检查返回的统计数据

**预期结果：**
- ✅ 状态码：200
- ✅ 返回数据包含：
  - `totalRecords`（总记录数）
  - `totalDistanceKilometers`（总距离）
  - `totalCarbonEmission`（总碳排放）
  - `byTransportMode`（按出行方式统计的数组）

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

### 5. 筛选功能测试

#### 5.1 按出行方式筛选

**测试步骤：**
1. 测试 `GET /api/travel/my-travels?transportMode=0&page=1&pageSize=10`
2. 检查返回的结果

**预期结果：**
- ✅ 状态码：200
- ✅ 返回的所有记录的 `transportMode` 都应该是 0（步行）

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

#### 5.2 按日期范围筛选

**测试步骤：**
1. 测试 `GET /api/travel/my-travels?startDate=2024-01-01&endDate=2024-12-31`
2. 检查返回的结果

**预期结果：**
- ✅ 状态码：200
- ✅ 返回的记录都在指定日期范围内

**实际结果：** ☐ 通过 ☐ 失败

**备注：**

---

### 6. 不同出行方式的碳排放计算测试

#### 6.1 步行（TransportMode = 0）

**测试步骤：**
1. 预览路线，使用 `transportMode: 0`
2. 检查 `estimatedCarbonEmission`

**预期结果：**
- ✅ `estimatedCarbonEmission` = 0

**实际结果：** ☐ 通过 ☐ 失败

---

#### 6.2 自行车（TransportMode = 1）

**测试步骤：**
1. 预览路线，使用 `transportMode: 1`
2. 检查 `estimatedCarbonEmission`

**预期结果：**
- ✅ `estimatedCarbonEmission` = 0

**实际结果：** ☐ 通过 ☐ 失败

---

#### 6.3 地铁（TransportMode = 3）

**测试步骤：**
1. 预览路线，使用 `transportMode: 3`
2. 检查 `estimatedCarbonEmission`

**预期结果：**
- ✅ `estimatedCarbonEmission` > 0

**实际结果：** ☐ 通过 ☐ 失败

---

#### 6.4 公交车（TransportMode = 4）

**测试步骤：**
1. 预览路线，使用 `transportMode: 4`
2. 检查 `estimatedCarbonEmission`

**预期结果：**
- ✅ `estimatedCarbonEmission` > 0

**实际结果：** ☐ 通过 ☐ 失败

---

### 7. 错误处理测试

#### 7.1 无效地址

**测试步骤：**
1. 测试预览API，使用无效地址：
   ```json
   {
     "originAddress": "InvalidAddress123456789",
     "destinationAddress": "AnotherInvalidAddress",
     "transportMode": 3
   }
   ```

**预期结果：**
- ✅ 状态码：400
- ✅ 错误消息：包含 "Unable to geocode" 或类似信息

**实际结果：** ☐ 通过 ☐ 失败

---

#### 7.2 缺少必填字段

**测试步骤：**
1. 测试预览API，只提供部分字段：
   ```json
   {
     "originAddress": "Beijing"
   }
   ```

**预期结果：**
- ✅ 状态码：400
- ✅ 错误消息：包含 "validation failed" 或类似信息

**实际结果：** ☐ 通过 ☐ 失败

---

#### 7.3 未授权访问

**测试步骤：**
1. 不提供 Token，直接调用 API

**预期结果：**
- ✅ 状态码：401
- ✅ 错误消息：包含 "Unauthorized" 或类似信息

**实际结果：** ☐ 通过 ☐ 失败

---

#### 7.4 获取不存在的记录

**测试步骤：**
1. 测试 `GET /api/travel/99999`（不存在的ID）

**预期结果：**
- ✅ 状态码：404
- ✅ 错误消息：包含 "not found" 或类似信息

**实际结果：** ☐ 通过 ☐ 失败

---

### 8. 数据格式验证

#### 8.1 坐标格式

**测试步骤：**
1. 创建一条记录
2. 检查返回的坐标数据

**预期结果：**
- ✅ `originLatitude` 在 -90 到 90 之间
- ✅ `originLongitude` 在 -180 到 180 之间
- ✅ `destinationLatitude` 在 -90 到 90 之间
- ✅ `destinationLongitude` 在 -180 到 180 之间

**实际结果：** ☐ 通过 ☐ 失败

---

#### 8.2 Polyline 格式

**测试步骤：**
1. 创建一条记录
2. 检查返回的 `routePolyline`

**预期结果：**
- ✅ `routePolyline` 不为空
- ✅ `routePolyline` 是字符串格式
- ✅ 可以用于 Google Maps JavaScript API 绘制路线

**实际结果：** ☐ 通过 ☐ 失败

---

## 📊 测试总结

### 测试结果统计

- **总测试项：** 20+
- **通过：** ___ 项
- **失败：** ___ 项
- **未测试：** ___ 项

### 发现的问题

1. 
2. 
3. 

### 需要修复的问题

1. 
2. 
3. 

---

## ✅ 测试完成确认

- [ ] 所有基础功能测试通过
- [ ] 缓存功能正常工作
- [ ] 数据库操作正常
- [ ] 不同出行方式的碳排放计算正确
- [ ] 错误处理正常
- [ ] 数据格式符合前端需求

**测试人员：** _______________

**测试日期：** _______________

**测试结论：** ☐ 通过 ☐ 需要修复问题

---

## 📝 备注

如有任何问题或发现，请在此记录：
