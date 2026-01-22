# Google Maps API 快速测试指南

## 🚀 快速开始（5分钟测试）

### 步骤 1：启动项目

```powershell
cd .NET/EcoLens.Api
dotnet run
```

等待看到：
```
Now listening on: http://localhost:5133
Swagger UI available at: http://localhost:5133/swagger
```

### 步骤 2：打开 Swagger UI

在浏览器中打开：`http://localhost:5133/swagger`

### 步骤 3：获取 Token

1. 找到 `POST /api/auth/register`
2. 点击 "Try it out"
3. 输入：
   ```json
   {
     "username": "testuser",
     "email": "test@example.com",
     "password": "Test123!"
   }
   ```
4. 点击 "Execute"
5. 复制返回的 `token` 值

### 步骤 4：授权

1. 点击页面右上角的 "Authorize" 按钮
2. 输入：`Bearer {你的token}`
3. 点击 "Authorize"
4. 点击 "Close"

### 步骤 5：测试关键功能

#### ✅ 测试 1：预览路线（验证 Google Maps API）

1. 找到 `POST /api/travel/preview`
2. 点击 "Try it out"
3. 输入：
   ```json
   {
     "originAddress": "Beijing Chaoyang District",
     "destinationAddress": "Beijing Haidian District",
     "transportMode": 3
   }
   ```
4. 点击 "Execute"

**检查点：**
- ✅ 状态码：200
- ✅ 有 `originLatitude` 和 `originLongitude`
- ✅ 有 `destinationLatitude` 和 `destinationLongitude`
- ✅ `distanceKilometers` > 0
- ✅ `estimatedCarbonEmission` > 0
- ✅ `routePolyline` 不为空

#### ✅ 测试 2：创建记录（验证数据库和碳排放计算）

1. 找到 `POST /api/travel`
2. 点击 "Try it out"
3. 输入（步行，碳排放应为0）：
   ```json
   {
     "originAddress": "Tiananmen Square, Beijing",
     "destinationAddress": "Forbidden City, Beijing",
     "transportMode": 0,
     "notes": "Test"
   }
   ```
4. 点击 "Execute"

**检查点：**
- ✅ 状态码：200
- ✅ 返回 `id`（新创建的记录ID）
- ✅ `carbonEmission` = 0（步行应该为0）

#### ✅ 测试 3：获取列表（验证数据库查询）

1. 找到 `GET /api/travel/my-travels`
2. 点击 "Try it out"
3. 点击 "Execute"

**检查点：**
- ✅ 状态码：200
- ✅ 返回 `items` 数组
- ✅ 返回 `totalCount`

#### ✅ 测试 4：获取统计（验证统计功能）

1. 找到 `GET /api/travel/statistics`
2. 点击 "Try it out"
3. 点击 "Execute"

**检查点：**
- ✅ 状态码：200
- ✅ 返回 `totalRecords`
- ✅ 返回 `totalDistanceKilometers`
- ✅ 返回 `totalCarbonEmission`
- ✅ 返回 `byTransportMode` 数组

#### ✅ 测试 5：错误处理（验证错误场景）

1. 找到 `POST /api/travel/preview`
2. 点击 "Try it out"
3. 输入无效地址：
   ```json
   {
     "originAddress": "InvalidAddress123456",
     "destinationAddress": "AnotherInvalidAddress",
     "transportMode": 3
   }
   ```
4. 点击 "Execute"

**检查点：**
- ✅ 状态码：400
- ✅ 错误消息为英文

---

## ✅ 所有测试通过标准

如果以上5个测试都通过，说明：

1. ✅ Google Maps API 集成正常
2. ✅ 地址转坐标功能正常
3. ✅ 路线获取功能正常
4. ✅ 碳排放计算正常
5. ✅ 数据库操作正常
6. ✅ 统计功能正常
7. ✅ 错误处理正常

---

## 🔍 详细测试

如果需要更详细的测试，请参考：
- `GOOGLE_MAPS_API_TEST_CHECKLIST.md` - 完整的测试检查清单
- `GOOGLE_MAPS_API_TEST_SUMMARY.md` - 测试总结和说明

---

## ⚠️ 常见问题

### 问题 1：无法获取 Token

**解决方案：**
- 检查项目是否正常启动
- 检查数据库连接是否正常
- 查看服务器日志

### 问题 2：预览路线返回 400 错误

**可能原因：**
- Google Maps API Key 未配置或无效
- 地址无法解析
- API Key 配额已用完

**解决方案：**
- 检查 `appsettings.Development.json` 中的 API Key
- 检查 Google Cloud Console 中的 API 状态
- 尝试使用更明确的地址（如 "Beijing, China"）

### 问题 3：返回 401 错误

**解决方案：**
- 确保已点击 "Authorize" 按钮
- 确保 Token 格式正确：`Bearer {token}`
- 重新登录获取新 Token

---

## 📝 测试记录

**测试日期：** _______________

**测试结果：**
- [ ] 测试 1：预览路线 - ☐ 通过 ☐ 失败
- [ ] 测试 2：创建记录 - ☐ 通过 ☐ 失败
- [ ] 测试 3：获取列表 - ☐ 通过 ☐ 失败
- [ ] 测试 4：获取统计 - ☐ 通过 ☐ 失败
- [ ] 测试 5：错误处理 - ☐ 通过 ☐ 失败

**总体结论：** ☐ 所有测试通过 ☐ 需要修复问题

**备注：**
