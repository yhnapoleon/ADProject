# API 需求清单

| 模块/功能 | API 路径 | 方法 | 请求参数 (Input) | 期望响应字段 (Output) | 前端调用位置 (可选) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 用户登录 | `/api/auth/login` | POST | `email`(string, 必填), `password`(string, 必填) | `token`(string), `user.id`(string), `user.name`(string), `user.nickname`(string), `user.email`(string), `user.location`(string), `user.birthDate`(string, YYYY-MM-DD), `user.avatar`(string, url), `user.joinDays`(number), `user.pointsWeek`(number), `user.pointsMonth`(number), `user.pointsTotal`(number) | `web/src/pages/Login.tsx` |
| 用户注册 | `/api/auth/register` | POST | `username`(string, 必填), `email`(string, 必填), `password`(string, 必填), `dateOfBirth`(string, YYYY-MM-DD, 必填), `location`(string, 必填) | `user.id`(string), `user.name`(string), `user.email`(string), `user.location`(string), `user.birthDate`(string), 可选: `token`(string) | `web/src/pages/Register.tsx` |
| 获取当前用户信息 | `/api/me` | GET | 需携带 `Authorization: Bearer <token>` | `id`(string), `name`(string), `nickname`(string), `email`(string), `location`(string), `birthDate`(string), `avatar`(string), `joinDays`(number), `pointsWeek`(number), `pointsMonth`(number), `pointsTotal`(number) | `web/src/pages/Profile.tsx` |
| 更新个人资料 | `/api/me` | PUT | 需授权; `nickname`(string, 选填), `email`(string, 选填), `location`(string, 选填), `birthDate`(string, YYYY-MM-DD, 选填), `password`(string, 选填) | 同“获取当前用户信息”返回的用户字段（更新后的值） | `web/src/pages/Profile.tsx` |
| 更新头像 | `/api/me/avatar` | PUT | 需授权; `file`(multipart/form-data, 头像文件, 与下方二选一), 或 `avatarBase64`(string, base64) | `avatar`(string, url) | `web/src/pages/Profile.tsx` |
| 排行榜-列表 | `/api/leaderboard` | GET | `period`(enum: today/week/month/all, 选填, 默认 month), `limit`(number, 选填, 默认 50) | `items[]` 列表: `rank`(number), `username`(string), `nickname`(string), `emissions`(number), `avatarUrl`(string), `pointsToday?`(number), `pointsWeek`(number), `pointsMonth`(number), `pointsTotal`(number) | `web/src/pages/Leaderboard.tsx`, `web/src/pages/Dashboard.tsx` |
| 排行榜-单用户 | `/api/leaderboard/{username}` | GET | `username`(path, 必填) | 同排行榜条目字段 | `web/src/mock/data.ts`（现为 mock，用于推导需求） |
| 记录-查询 | `/api/records` | GET | 需授权; `type`(enum: Food/Transport/Utilities, 选填), `month`(string, YYYY-MM, 选填), `page`(number, 选填), `pageSize`(number, 选填) | `items[]`: `id`(string), `date`(string, YYYY-MM-DD), `type`(enum), `amount`(number), `unit`(string, 如 "kg CO₂e"), `description`(string); `total`(number) | `web/src/pages/Records.tsx` |
| 记录-删除 | `/api/records/{id}` | DELETE | 需授权; `id`(path, 必填) | `success`(boolean) | `web/src/pages/Records.tsx` |
| 记录-创建(食物) | `/api/records/food` | POST | 需授权; `foodName`(string, 必填), `amount`(number, 必填, 单位 kg), `note`(string, 选填), `image`(multipart/form-data, 选填) | 创建后的 `Record`: `id`(string), `date`(string), `type`("Food"), `amount`(number, 计算后的 CO₂e), `unit`(string, "kg CO₂e"), `description`(string) | `web/src/pages/LogMeal.tsx` |
| 记录-创建(出行) | `/api/records/travel` | POST | 需授权; `mode`(enum: airplane/bus/cycle/car/ship/mrt, 必填), `origin`(string, 必填), `destination`(string, 必填), `distance`(number, km, 选填-如后端可计算则选填), `note`(string, 选填) | 创建后的 `Record`: `id`, `date`, `type`("Transport"), `amount`(number, CO₂e), `unit`("kg CO₂e"), `description`(string) | `web/src/pages/LogTravel.tsx` |
| 记录-创建(水电) | `/api/records/utilities` | POST | 需授权; `electricityUsage`(number, kWh, 必填), `waterUsage`(number, 立方米, 必填), `month`(string, YYYY-MM, 必填), `note`(string, 选填) | 创建后的 `Record`: `id`, `date`(可用月初), `type`("Utilities"), `amount`(number, 合计 CO₂e), `unit`("kg CO₂e"), `description`(string) | `web/src/pages/LogUtility.tsx` |
| 管理员登录 | `/api/admin/auth/login` | POST | `username`(string, 必填), `password`(string, 必填) | `token`(string), `admin.username`(string) | `web/src/pages/AdminLogin.tsx` |
| 管理-用户列表 | `/api/admin/users` | GET | 需管理员授权; `search`(string, 选填) | `items[]`: `id`(string), `username`(string), `email`(string), `joinedDate`(string, YYYY-MM-DD), `totalReduction`(number, kg), `points`(number), `status`(string: Active/Banned) | `web/src/pages/AdminUserList.tsx` |
| 管理-批量更新用户 | `/api/admin/users/batch` | PATCH | 需管理员授权; `updates[]`(array, 必填)：每项 `{ id`(string, 必填)`, points`(number, 选填)`, status`(string, 选填)`} | `updated`(number), `failed`(number) | `web/src/pages/AdminUserList.tsx` |
| 管理-排放因子列表 | `/api/admin/emission-factors` | GET | 需管理员授权; `category`(string, 选填), `search`(string, 选填), `page`/`pageSize`(选填) | `items[]`: `id`(string), `category`(string), `itemName`(string), `factor`(number), `unit`(string), `source`(string), `status`(string: Draft/Review Pending/Published), `lastUpdated`(string, YYYY-MM-DD); `total`(number) | `web/src/pages/AdminEmissionFactors.tsx` |
| 管理-新增排放因子 | `/api/admin/emission-factors` | POST | 需管理员授权; `id`(string, 必填), `category`(string, 必填), `itemName`(string, 必填), `factor`(number, 必填), `unit`(string, 必填), `source`(string, 选填), `status`(string, 必填) | 创建后的因子对象（同上字段） | `web/src/pages/AdminEmissionFactors.tsx` |
| 管理-更新排放因子 | `/api/admin/emission-factors/{id}` | PUT | 需管理员授权; 路径 `id`(string), body 同上字段(选填) | 更新后的因子对象 | `web/src/pages/AdminEmissionFactors.tsx` |
| 管理-批量导入排放因子 | `/api/admin/emission-factors/import` | POST | 需管理员授权; `file`(multipart/form-data, .csv/.json, 必填) | `imported`(number), `failed`(number), `errors[]`(string[]) | `web/src/pages/AdminEmissionFactors.tsx` |
| 区域碳减排统计 | `/api/regions/stats` | GET | （管理员使用页面）可加缓存; 无或 `region`(string[], 选填) | `items[]`: `regionCode`(string), `regionName`(string), `carbonReduced`(number, kg), `userCount`(number), `reductionRate`(number, %) | `web/src/pages/AdminDashboard.tsx` |
| 平台周度影响指标 | `/api/admin/impact/weekly` | GET | 需管理员授权 | `items[]`: `week`(string), `value`(number) | `web/src/pages/AdminDashboard.tsx` |
| 社区分析-分类占比 | `/api/admin/analytics/category-share` | GET | 需管理员授权 | `items[]`: `name`(string: Energy/Transport/Food/Goods & Services...), `value`(number, %) | `web/src/pages/AdminCommunityAnalytics.tsx` |
| 社区分析-活跃增长 | `/api/admin/analytics/engagement` | GET | 需管理员授权 | `items[]`: `month`(string, Mon), `dau`(number), `mau`(number) | `web/src/pages/AdminCommunityAnalytics.tsx` |
| 账单识别（OCR/推断） | `/api/vision/utility-bill` | POST | 需授权; `file`(multipart/form-data, 必填) | `electricityUsage?`(number, kWh), `waterUsage?`(number, 立方米), `month?`(string, YYYY-MM) | `web/src/pages/LogUtility.tsx` |
| 餐食识别（AI） | `/api/vision/meal-photo` | POST | 需授权; `file`(multipart/form-data, 必填) | `foodName`(string), `amount?`(number, kg), `confidence?`(number, 0-1) | `web/src/pages/LogMeal.tsx` |
| 管理-系统设置-获取 | `/api/admin/settings` | GET | 需管理员授权 | `confidenceThreshold`(number, 0-100), `visionModel`(string), `weeklyDigest`(boolean), `maintenanceMode`(boolean) | `web/src/pages/AdminSettings.tsx` |
| 管理-系统设置-更新 | `/api/admin/settings` | PUT | 需管理员授权; `confidenceThreshold?`(number), `visionModel?`(string), `weeklyDigest?`(boolean), `maintenanceMode?`(boolean) | 同上（更新后的值） | `web/src/pages/AdminSettings.tsx` |
| AI 助手对话 | `/api/assistant/chat` | POST | 需授权; `messages[]`(array, `{ role: "user"|"assistant", content: string }`), 可选 `stream`(boolean) | `message`(object, `{ role: "assistant", content: string }`) 或流式分片 | `web/src/pages/AIAssistant.tsx` |

附加说明：
- 前端 axios 基础配置位于 `web/src/utils/request.ts`，默认 `baseURL = import.meta.env.VITE_API_URL || '/api'`，超时 10s；响应拦截器返回 `response.data`。如后端采用统一返回包裹，建议格式：`{ code: number, message: string, data: any }`（参考 `web/src/types/request.d.ts` 的 `ApiResponse<T>`）。
- 需要鉴权的接口请通过 `Authorization: Bearer <token>` 传递；管理员接口需使用管理员令牌。
- `Record`、`LeaderboardEntry`、`User` 等字段参考 `web/src/types/index.ts`，本文档已按前端实际使用字段列出为“必需输出字段”。如后端有更多字段，可在不破坏现有字段含义的前提下扩展。

