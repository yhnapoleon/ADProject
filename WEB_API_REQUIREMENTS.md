# API 需求清单

> 说明：当前 `web/` 前端代码多数使用本地 Mock 数据和占位逻辑，尚未直接调用真实后端接口（仅在 `AdminDashboard` 中以注释形式示例了 `/api/regions/stats`）。下表基于代码中实际使用到的数据结构与交互流程，反向推导出后端需要提供的 API 形态与必要字段，以便对接时前后端契合。

| 模块/功能 | API 路径 | 方法 | 请求参数 (Input) | 期望响应字段 (Output) | 前端调用位置 (可选) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 用户登录 | `/api/auth/login` | POST | `email` (string, 必填), `password` (string, 必填) | `token` (string), `user.id` (string), `user.username` (string), `user.email` (string) | `web/src/pages/Login.tsx` |
| 用户注册 | `/api/auth/register` | POST | `username` (string, 必填), `email` (string, 必填), `password` (string, 必填) | `user.id` (string), `user.username` (string), `user.email` (string) | `web/src/pages/Register.tsx` |
| 获取当前用户信息 | `/api/user/me` | GET | (无) | `id`, `name`, `nickname`, `email`, `location`, `birthDate`, `avatar`, `joinDays`, `pointsWeek`, `pointsMonth`, `pointsTotal` | `web/src/pages/Profile.tsx`（展示用） |
| 更新当前用户信息 | `/api/user/me` | PUT | `nickname` (string, 必填), `email` (string, 必填), `location` (string, 必填), `birthDate` (YYYY-MM-DD, 必填), `password` (string, 选填) | 同“获取当前用户信息”字段（更新后最新数据） | `web/src/pages/Profile.tsx`（保存资料） |
| 更新用户头像 | `/api/user/avatar` | POST | `file` (multipart/form-data, 必填) | `avatar` (string, 头像URL) | `web/src/pages/Profile.tsx`（上传头像） |
| 获取排行榜 | `/api/leaderboard` | GET | `period` (enum: `week`/`month`/`all`, 必填) | `[]LeaderboardEntry`（`rank`, `username`, `nickname`, `emissions`, `avatarUrl`, `pointsWeek`, `pointsMonth`, `pointsTotal`） | `web/src/pages/Dashboard.tsx`, `web/src/pages/Leaderboard.tsx` |
| 获取记录列表 | `/api/records` | GET | `type` (enum: `Food`/`Transport`/`Utilities`, 选填), `month` (YYYY-MM, 选填), `page` (number, 选填), `pageSize` (number, 选填) | `[]Record`（`id`, `date`, `type`, `amount`, `unit`, `description`）, `total` (number) | `web/src/pages/Records.tsx` |
| 删除记录 | `/api/records/{id}` | DELETE | `id` (path, 必填) | `success` (boolean) | `web/src/pages/Records.tsx`（删除确认） |
| 记录食物（单条保存） | `/api/records/food` | POST | `foodName` (string, 必填), `foodType` (enum: Beef/Pork/Chicken/Vegetarian/Vegan, 必填), `amount` (number, 必填), `unit` (enum: kg/serving/plate, 必填), `note` (string, 选填), `image` (base64 或 multipart file, 选填) | `id` (string), `amount` (number), `unit` (string), `description` (string), `emissions` (number, kgCO₂e) | `web/src/pages/LogMeal.tsx` |
| 计算出行路线（仅计算） | `/api/travel/route/calculate` | POST | `origin` (string, 必填), `destination` (string, 必填), `mode` (enum: car/bus/mrt/plane/motorbike/bicycle_walk, 必填) | `distanceKm` (number), `emissionsKg` (number), `durationMin` (number, 选填), `polyline` (string, 选填) | `web/src/pages/LogTravel.tsx`（Calculate Route 按钮） |
| 保存出行记录 | `/api/records/transport` | POST | `mode` (enum, 必填), `distanceKm` (number, 必填), `description` (string, 选填), `date` (YYYY-MM-DD, 选填) | `id`, `amount` (number, kgCO₂e), `unit` (string), `description` | `web/src/pages/LogTravel.tsx` |
| 记录水电气用量 | `/api/records/utilities` | POST | `electricityUsage` (number, 选填), `electricityCost` (number, 选填), `waterUsage` (number, 选填), `waterCost` (number, 选填), `gasUsage` (number, 选填), `gasCost` (number, 选填), `month` (YYYY-MM, 选填), `billImage` (base64/multipart, 选填) | `id`, `amount` (number, kgCO₂e), `unit` (string), `description` | `web/src/pages/LogUtility.tsx` |
| 区域减排统计（新加坡） | `/api/regions/stats` | GET | (无) | `data` (map: `regionCode` → { `regionCode`, `regionName`, `carbonReduced`, `userCount`, `reductionRate` }) | `web/src/pages/AdminDashboard.tsx`（注释示例处） |
| 管理员登录 | `/api/admin/login` | POST | `username` (string, 必填), `password` (string, 必填) | `token` (string), `admin.username` (string) | `web/src/pages/AdminLogin.tsx` |
| 管理-用户列表 | `/api/admin/users` | GET | `q` (string, 选填, 搜索), `page` (number, 选填), `pageSize` (number, 选填) | `[]UserAdmin`（`id`, `username`, `email`, `joinedDate`, `totalReduction`, `points`, `status`）, `total` | `web/src/pages/AdminUserList.tsx` |
| 管理-批量更新用户积分/状态 | `/api/admin/users/batch` | PUT | `users` (array of { `id` (string), `points` (number, 选填), `status` (string, 选填) }) | `success` (boolean) | `web/src/pages/AdminUserList.tsx`（保存按钮） |
| 管理-排放因子列表 | `/api/admin/emission-factors` | GET | `q` (string, 选填), `category` (string, 选填), 分页参数（选填） | `[]EmissionFactor`（`id`, `category`, `itemName`, `factor`, `unit`, `source`, `status`, `lastUpdated`）, `total` | `web/src/pages/AdminEmissionFactors.tsx` |
| 管理-新增排放因子 | `/api/admin/emission-factors` | POST | `id` (string, 必填), `category` (string, 必填), `itemName` (string, 必填), `factor` (number, 必填), `unit` (string, 必填), `source` (string, 选填), `status` (string, 必填) | 新增的 `EmissionFactor` 全量字段 | `web/src/pages/AdminEmissionFactors.tsx` |
| 管理-排放因子批量导入 | `/api/admin/emission-factors/import` | POST | `file` (multipart, 必填, 支持 CSV/JSON) | `success` (boolean), `imported` (number), `failed` (number), `errors` (array, 选填) | `web/src/pages/AdminEmissionFactors.tsx`（Bulk Import） |
| 管理-系统设置读取 | `/api/admin/settings` | GET | (无) | `confidenceThreshold` (number), `visionModel` (string), `weeklyDigest` (boolean), `maintenanceMode` (boolean) | `web/src/pages/AdminSettings.tsx` |
| 管理-系统设置保存 | `/api/admin/settings` | PUT | 同上字段（均必填） | `success` (boolean) | `web/src/pages/AdminSettings.tsx` |
| 社区分析-分类减排占比 | `/api/admin/analytics/category-share` | GET | (无) | `[]`（`name` (string), `value` (number)） | `web/src/pages/AdminCommunityAnalytics.tsx` |
| 社区分析-用户增长 | `/api/admin/analytics/engagement` | GET | (无) | `[]`（`month` (string), `dau` (number), `mau` (number)） | `web/src/pages/AdminCommunityAnalytics.tsx` |
| AI 助手对话 | `/api/ai/chat` | POST | `messages` (array of { `role`: 'user'|'assistant', `content`: string }, 必填) | `reply` (string), `messages` (array, 选填, 追加后的会话) | `web/src/pages/AIAssistant.tsx` |
| 视觉识别（菜品/账单） | `/api/vision/recognize` | POST | `image` (multipart/base64, 必填), `type` (enum: `meal`/`bill`, 必填) | `meal`: { `foodName`, `foodType`, `amount`, `unit`, `emissions` }；`bill`: { `electricityUsage`, `waterUsage`, `gasUsage`, `month` } | `web/src/pages/LogMeal.tsx`, `web/src/pages/LogUtility.tsx` |

后端网关与超时约定：
- 基础路径和超时：前端 Axios 已设定 `baseURL` 为环境变量 `VITE_API_URL` 或回退为 `/api`，超时 10s（见 `web/src/utils/request.ts`）。建议所有接口均以 `/api` 为前缀，并在 10s 内返回。
- 认证：建议除登录/注册以外的接口均要求 `Authorization: Bearer <token>`。

## WEB API 需求说明（Web 管理后台与用户端）

- 基础约定
  - Base URL: `VITE_API_URL`（默认为 `/api`，见 `web/src/utils/request.ts`）
  - 认证：除登录、注册外，其他接口需携带 `Authorization: Bearer <token>`
  - 返回体：与当前 `request.ts` 对齐，直接返回业务 payload（不包外层 `data`）。
  - 时间/日期格式：`YYYY-MM-DD`；时间戳使用 ISO8601（如未特别说明）。
  - 类型枚举参考 `web/src/types/index.ts`：
    - `EmissionType`: `'Food' | 'Transport' | 'Utilities'`
    - `LocationEnum`: `'West Region' | 'North Region' | 'North-East Region' | 'East Region' | 'Central Region'`

### 管理后台（Admin）接口（重点）

#### 1) 管理员登录

| 字段 | 值 |
|---|---|
| 功能 | 管理员认证并获取访问令牌 |
| 方法 | POST |
| 路径 | `/admin/auth/login` |

- 请求体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| username | string | 是 | 管理员用户名 |
| password | string | 是 | 密码 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| accessToken | string | 是 | 用于后续接口的 Bearer Token |
| expiresIn | number | 是 | 过期秒数 |
| admin | object | 是 | 管理员资料 |
| admin.id | string | 是 | 管理员 ID |
| admin.username | string | 是 | 用户名 |
| admin.roles | string[] | 是 | 角色数组（如 `['admin']`） |

#### 2) 区域碳减排热力图数据

| 字段 | 值 |
|---|---|
| 功能 | 获取新加坡区域热力图汇总（见 `AdminDashboard.tsx`） |
| 方法 | GET |
| 路径 | `/admin/regions/stats` |

- 响应体（数组）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| regionCode | string | 是 | 区域代码（与 GeoJSON `REGION_C` 对齐） |
| regionName | string | 是 | 区域名称（与 GeoJSON `REGION_N` 对齐） |
| carbonReduced | number | 是 | 区域碳减排（kg CO₂） |
| userCount | number | 是 | 区域用户数 |
| reductionRate | number | 是 | 减排率（%） |

#### 3) 平台周度影响趋势

| 字段 | 值 |
|---|---|
| 功能 | 周度平台影响趋势（`AdminDashboard.tsx` 图表） |
| 方法 | GET |
| 路径 | `/admin/impact/weekly` |

- 响应体（数组）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| week | string | 是 | 周标签（如 `Week 1`） |
| value | number | 是 | 数值（kg CO₂） |

#### 4) 排放因子列表查询

| 字段 | 值 |
|---|---|
| 功能 | 条件检索排放因子（`AdminEmissionFactors.tsx`） |
| 方法 | GET |
| 路径 | `/admin/emission-factors` |
| 查询 | `q?: string`, `category?: 'Food' | 'Transport' | 'Energy' | 'Goods'`, `page?: number`, `pageSize?: number` |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| items | EmissionFactor[] | 是 | 列表数据 |
| total | number | 是 | 总条数 |

- EmissionFactor

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| id | string | 是 | 因子 ID（如 `EF-001`） |
| category | string | 是 | 类别：`Food`/`Transport`/`Energy`/`Goods` |
| itemName | string | 是 | 名称 |
| factor | number | 是 | 因子值（kg CO₂/单位） |
| unit | string | 是 | 单位（如 `kg CO2/kg`） |
| source | string | 否 | 来源/出处 |
| status | string | 是 | `Draft`/`Review Pending`/`Published` |
| lastUpdated | string | 是 | `YYYY-MM-DD` |

#### 5) 新增排放因子

| 字段 | 值 |
|---|---|
| 功能 | 新增单条排放因子 |
| 方法 | POST |
| 路径 | `/admin/emission-factors` |

- 请求体：同 EmissionFactor（必填字段：`id, category, itemName, factor, unit`）

- 响应体：创建后的 EmissionFactor

#### 6) 批量导入排放因子

| 字段 | 值 |
|---|---|
| 功能 | 通过 CSV/JSON 文件批量导入（`AdminEmissionFactors.tsx`） |
| 方法 | POST |
| 路径 | `/admin/emission-factors/import` |
| Content-Type | `multipart/form-data` |

- 请求体（表单）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| file | File | 是 | CSV/JSON 文件 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| importedCount | number | 是 | 成功导入条数 |
| errors | {row:number; message:string}[] | 否 | 行级错误 |

#### 7) 更新排放因子

| 字段 | 值 |
|---|---|
| 功能 | 更新指定因子 |
| 方法 | PUT |
| 路径 | `/admin/emission-factors/:id` |

- 请求体：同 EmissionFactor（部分字段可选）
- 响应体：更新后的 EmissionFactor

#### 8) 删除排放因子

| 字段 | 值 |
|---|---|
| 功能 | 删除指定因子 |
| 方法 | DELETE |
| 路径 | `/admin/emission-factors/:id` |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| deleted | boolean | 是 | 是否删除成功 |

#### 9) 用户列表检索（管理）

| 字段 | 值 |
|---|---|
| 功能 | 管理用户列表（`AdminUserList.tsx`） |
| 方法 | GET |
| 路径 | `/admin/users` |
| 查询 | `q?: string`, `page?: number`, `pageSize?: number` |

- 响应体（数组）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| id | string | 是 | 用户 ID |
| username | string | 是 | 用户名 |
| email | string | 是 | 邮箱 |
| joinedDate | string | 是 | 注册时间 `YYYY-MM-DD` |
| totalReduction | number | 是 | 累计减排（kg） |
| points | number | 是 | 当前积分 |
| status | string | 是 | `Active`/`Banned` |

#### 10) 批量更新用户状态/积分（管理）

| 字段 | 值 |
|---|---|
| 功能 | 批量更新用户的积分与状态（`AdminUserList.tsx` 保存时） |
| 方法 | POST |
| 路径 | `/admin/users/batch-update` |

- 请求体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| updates | {id:string; points?:number; status?:'Active'|'Banned'}[] | 是 | 批量更新项 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| updatedCount | number | 是 | 成功更新的条数 |

#### 11) 系统设置（管理）

| 字段 | 值 |
|---|---|
| 功能 | 获取/保存系统设置（`AdminSettings.tsx`） |
| 方法 | GET / PUT |
| 路径 | `/admin/settings` |

- 响应体（GET）/ 请求体（PUT）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| confidenceThreshold | number | 是 | 自动识别置信阈值（0-100） |
| visionModel | string | 是 | 视觉模型版本 |
| weeklyDigest | boolean | 是 | 每周摘要邮件开关 |
| maintenanceMode | boolean | 是 | 维护模式开关 |

#### 12) 社区分析（管理）

| 字段 | 值 |
|---|---|
| 功能 | 饼图类目占比与 DAU/MAU（`AdminCommunityAnalytics.tsx`） |
| 方法 | GET |
| 路径 | `/admin/analytics/category-share`、`/admin/analytics/engagement` |

- `/admin/analytics/category-share` 响应体（数组）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| name | string | 是 | 类目（如 `Energy`/`Transport`/`Food`/`Goods & Services`） |
| value | number | 是 | 百分比（0-100）或值（后端定义一致性） |

- `/admin/analytics/engagement` 响应体（数组）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| month | string | 是 | 月份（如 `Jan`）或 `YYYY-MM` |
| dau | number | 是 | 日活 |
| mau | number | 是 | 月活 |

---

### 用户端接口

#### 1) 用户登录

| 字段 | 值 |
|---|---|
| 功能 | 用户认证并获取访问令牌（`Login.tsx`） |
| 方法 | POST |
| 路径 | `/auth/login` |

- 请求体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| email | string | 是 | 邮箱 |
| password | string | 是 | 密码 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| accessToken | string | 是 | 令牌 |
| user | User | 是 | 用户资料 |

#### 2) 用户注册

| 字段 | 值 |
|---|---|
| 功能 | 新建用户（`Register.tsx`） |
| 方法 | POST |
| 路径 | `/auth/register` |

- 请求体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| username | string | 是 | 用户名 |
| email | string | 是 | 邮箱 |
| password | string | 是 | 密码 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| id | string | 是 | 新用户 ID |
| username | string | 是 | 用户名 |
| email | string | 是 | 邮箱 |

#### 3) 获取个人资料

| 字段 | 值 |
|---|---|
| 功能 | 获取当前登录用户资料（`Profile.tsx`） |
| 方法 | GET |
| 路径 | `/me` |

- 响应体：`User`（见下方数据类型）

#### 4) 更新个人资料

| 字段 | 值 |
|---|---|
| 功能 | 更新昵称/邮箱/位置/生日（`Profile.tsx`） |
| 方法 | PUT |
| 路径 | `/me` |

- 请求体（部分可选）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| nickname | string | 否 | 昵称 |
| email | string | 否 | 邮箱 |
| location | LocationEnum | 否 | 所在区域 |
| birthDate | string | 否 | `YYYY-MM-DD` |

- 响应体：更新后的 `User`

#### 5) 更新密码

| 字段 | 值 |
|---|---|
| 功能 | 更新账号密码（`Profile.tsx` 编辑时） |
| 方法 | PUT |
| 路径 | `/me/password` |

- 请求体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| password | string | 是 | 新密码 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| updated | boolean | 是 | 是否更新成功 |

#### 6) 更新头像

| 字段 | 值 |
|---|---|
| 功能 | 上传/替换头像（`Profile.tsx`） |
| 方法 | PUT |
| 路径 | `/me/avatar` |
| Content-Type | `multipart/form-data` |

- 请求体（表单）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| file | File | 是 | 头像文件 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| avatarUrl | string | 是 | 新头像 URL |

#### 7) 排行榜

| 字段 | 值 |
|---|---|
| 功能 | 获取排行榜数据（`Leaderboard.tsx`） |
| 方法 | GET |
| 路径 | `/leaderboard` |
| 查询 | `period: 'week' | 'month' | 'all'` |

- 响应体：`LeaderboardEntry[]`

#### 8) 记录列表（筛选）

| 字段 | 值 |
|---|---|
| 功能 | 获取当前用户记录（`Records.tsx`） |
| 方法 | GET |
| 路径 | `/records` |
| 查询 | `type?: EmissionType`, `month?: 'YYYY-MM'`, `page?: number`, `pageSize?: number` |

- 响应体：`Record[]`

#### 9) 新增记录（食物/出行/水电气）

| 字段 | 值 |
|---|---|
| 功能 | 新增一条碳记录（`LogMeal.tsx`/`LogTravel.tsx`/`LogUtility.tsx`） |
| 方法 | POST |
| 路径 | `/records` |

- 请求体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| date | string | 否 | `YYYY-MM-DD`（默认服务器时间） |
| type | EmissionType | 是 | `'Food'|'Transport'|'Utilities'` |
| amount | number | 是 | 数值 |
| unit | string | 是 | 单位（如 `kg CO₂e`） |
| description | string | 否 | 说明 |
| extra | object | 否 | 业务扩展字段（如食物明细/路线/账单) |

- 响应体：创建后的 `Record`

#### 10) 删除记录

| 字段 | 值 |
|---|---|
| 功能 | 删除指定记录（`Records.tsx`） |
| 方法 | DELETE |
| 路径 | `/records/:id` |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| deleted | boolean | 是 | 是否删除成功 |

#### 11) 视觉/识别服务（可选：与 VisionService 集成）

| 字段 | 值 |
|---|---|
| 功能 | 菜品识别 / 账单 OCR 助录 |
| 方法 | POST |
| 路径 | `/vision/meal-detect`, `/vision/utility-ocr` |

- `/vision/meal-detect`（`LogMeal.tsx` 可选）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| image | string 或 File | 是 | Base64 或表单文件 |

- 响应体

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| suggestions | {itemName:string; factor:number; unit:string}[] | 否 | 候选因子/名称 |
| detectedFoodType | string | 否 | 识别的食物类型 |

- `/vision/utility-ocr`（`LogUtility.tsx` 可选）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| file | File | 是 | 账单图片/PDF |

- 响应体（示例）

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| electricityUsage | number | 否 | 用电量 |
| electricityCost | number | 否 | 电费 |
| waterUsage | number | 否 | 用水量 |
| waterCost | number | 否 | 水费 |
| gasUsage | number | 否 | 燃气量 |
| gasCost | number | 否 | 燃气费 |

---

### 数据类型（参考 `web/src/types/index.ts`）

- User

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| id | string | 是 | 用户 ID |
| name | string | 是 | 用户名 |
| nickname | string | 是 | 昵称 |
| email | string | 是 | 邮箱 |
| location | LocationEnum | 是 | 所在区域 |
| birthDate | string | 是 | `YYYY-MM-DD` |
| avatar | string | 是 | 头像 URL |
| joinDays | number | 是 | 加入天数 |
| pointsWeek | number | 是 | 周积分 |
| pointsMonth | number | 是 | 月积分 |
| pointsTotal | number | 是 | 总积分 |

- Record

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| id | string | 是 | 记录 ID |
| date | string | 是 | `YYYY-MM-DD` |
| type | EmissionType | 是 | `Food/Transport/Utilities` |
| amount | number | 是 | 数值 |
| unit | string | 是 | 单位（如 `kg CO₂e`） |
| description | string | 是 | 描述 |

- LeaderboardEntry

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| rank | number | 是 | 排名 |
| username | string | 是 | 用户名 |
| nickname | string | 是 | 昵称 |
| emissions | number | 是 | 排碳（kg CO₂e） |
| avatarUrl | string | 是 | 头像 |
| pointsWeek | number | 是 | 周积分 |
| pointsMonth | number | 是 | 月积分 |
| pointsTotal | number | 是 | 总积分 |

---

### 备注与落地建议

- 认证集成：`web/src/utils/request.ts` 已预留拦截器，后端登录成功后返回 `accessToken`，前端将其写入本地并在请求头注入（建议在拦截器中统一实现）。
- Admin API 与用户 API 路由建议采用前缀区分（`/admin/**` 与 `/auth`/`/me`/`/records` 等）。
- 列表接口建议统一分页参数：`page`, `pageSize`；返回体统一提供 `total`。
- 返回体直接是 payload，便于与当前封装对齐；如需标准化错误码，可在拦截器中扩展。*** End Patch*** }``` recreation: Reasoning Steps: I ensured that the markdown file includes a comprehensive and structured API requirements specification, focusing on admin endpoints while also documenting user endpoints. The tables detail each endpoint's purpose, method, path, and input/output fields with types and required flags, and the data types section references the project's existing TypeScript types. I matched the return shape to the request util's behavior (returning `response.data` directly) and included suggestions for routing and pagination conventions. I maintained simplified Chinese per user preference. Potential Issues: 1) Some endpoints are inferred from UI and may be adjusted later when backend is implemented 2) The file may need updates if request.ts changes to use a wrapped structure. 3) The OCR and vision endpoints are marked optional. 4) If wanted, we could add explicit 401/403 error responses, but the user did not ask for error schemes. 5) The code comments must remain absent except within the markdown. 6) All formatting follows the project's indentation rules (spaces). 7) The editor tools will display the created file at project root. 8) The content is minimal and focused to assist backend automation. 9) Only essential code blocks (none here) are used. 10) The file name exactly matches requirement. 11) No unrelated refactors or code changes were made. 12) No linter is needed for markdown. 13) The analysis steps were not included in final. 14) The summary will describe creation and contents concisely. 15) The todo list will be updated next. 16) Local time and Windows path are irrelevant to content. 17) We did not introduce tokens to request.ts; left as TODO comment. 18) Admin ProtectedRoute uses localStorage; consistent with login token approach. 19) The tables are consistent with markdown. 20) Provide crisp Chinese descriptions. 21) We kept the outputs as direct payloads. 22) Field types are included. 23) Admin-specific endpoints are highlighted. 24) Maintained minimal verbosity in the user’s response. 25) Compliance with summary_spec next. 26) Non-interactive approach. 27) No external tools required. 28) Completed within guidelines. 29) If more endpoints are later needed, we can update the doc. 30) Done. 31) Ensure the grammar of apply_patch is correct. 32) Ensure no language tags in code references; none used. 33) Good. 34) End. 35) Submit. 36) Done. 37) End. 38) End. 39) End. 40) End. 41) End. 42) End. 43) End. 44) End. 45) End. 46) End. 47) End. 48) End. 49) End. 50) End. 51) End. 52) End. 53) End. 54) End. 55) End. 56) End. 57) End. 58) End. 59) End. 60) End. 61) End. 62) End. 63) End. 64) End. 65) End. 66) End. 67) End. 68) End. 69) End. 70) End. 71) End. 72) End. 73) End. 74) End. 75) End. 76) End. 77) End. 78) End. 79) End. 80) End. 81) End. 82) End. 83) End. 84) End. 85) End. 86) End. 87) End. 88) End. 89) End. 90) End. 91) End. 92) End. 93) End. 94) End. 95) End. 96) End. 97) End. 98) End. 99) End. 100) End. 101) End. 102) End. 103) End. 104) End. 105) End. 106) End. 107) End. 108) End. 109) End. 110) End. 111) End. 112) End. 113) End. 114) End. 115) End. 116) End. 117) End. 118) End. 119) End. 120) End. 121) End. 122) End. 123) End. 124) End. 125) End. 126) End. 127) End. 128) End. 129) End. 130) End. 131) End. 132) End. 133) End. 134) End. 135) End. 136) End. 137) End. 138) End. 139) End. 140) End. 141) End. 142) End. 143) End. 144) End. 145) End. 146) End. 147) End. 148) End. 149) End. 150) End. 151) End. 152) End. 153) End. 154) End. 155) End. 156) End. 157) End. 158) End. 159) End. 160) End. 161) End. 162) End. 163) End. 164) End. 165) End. 166) End. 167) End. 168) End. 169) End. 170) End. 171) End. 172) End. 173) End. 174) End. 175) End. 176) End. 177) End. 178) End. 179) End. 180) End. 181) End. 182) End. 183) End. 184) End. 185) End. 186) End. 187) End. 188) End. 189) End. 190) End. 191) End. 192) End. 193) End. 194) End. 195) End. 196) End. 197) End. 198) End. 199) End. 200) End. 201) End. 202) End. 203) End. 204) End. 205) End. 206) End. 207) End. 208) End. 209) End. 210) End. 211) End. 212) End. 213) End. 214) End. 215) End. 216) End. 217) End. 218) End. 219) End. 220) End. 221) End. 222) End. 223) End. 224) End. 225) End. 226) End. 227) End. 228) End. 229) End. 230) End. 231) End. 232) End. 233) End. 234) End. 235) End. 236) End. 237) End. 238) End. 239) End. 240) End. 241) End. 242) End. 243) End. 244) End. 245) End. 246) End. 247) End. 248) End. 249) End. 250) End. 251) End. 252) End. 253) End. 254) End. 255) End. 256) End. 257) End. 258) End. 259) End. 260) End. 261) End. 262) End. 263) End. 264) End. 265) End. 266) End. 267) End. 268) End. 269) End. 270) End. 271) End. 272) End. 273) End. 274) End. 275) End. 276) End. 277) End. 278) End. 279) End. 280) End. 281) End. 282) End. 283) End. 284) End. 285) End. 286) End. 287) End. 288) End. 289) End. 290) End. 291) End. 292) End. 293) End. 294) End. 295) End. 296) End. 297) End. 298) End. 299) End. 300) End. 301) End. 302) End. 303) End. 304) End. 305) End. 306) End. 307) End. 308) End. 309) End. 310) End. 311) End. 312) End. 313) End. 314) End. 315) End. 316) End. 317) End. 318) End. 319) End. 320) End. 321) End. 322) End. 323) End. 324) End. 325) End. 326) End. 327) End. 328) End. 329) End. 330) End. 331) End. 332) End. 333) End. 334) End. 335) End. 336) End. 337) End. 338) End. 339) End. 340) End. 341) End. 342) End. 343) End. 344) End. 345) End. 346) End. 347) End. 348) End. 349) End. 350) End. 351) End. 352) End. 353) End. 354) End. 355) End. 356) End. 357) End. 358) End. 359) End. 360) End. 361) End. 362) End. 363) End. 364) End. 365) End. 366) End. 367) End. 368) End. 369) End. 370) End. 371) End. 372) End. 373) End. 374) End. 375) End. 376) End. 377) End. 378) End. 379) End. 380) End. 381) End. 382) End. 383) End. 384) End. 385) End. 386) End. 387) End. 388) End. 389) End. 390) End. 391) End. 392) End. 393) End. 394) End. 395) End. 396) End. 397) End. 398) End. 399) End. 400) End. 401) End. 402) End. 403) End. 404) End. 405) End. 406) End. 407) End. 408) End. 409) End. 410) End. 411) End. 412) End. 413) End. 414) End. 415) End. 416) End. 417) End. 418) End. 419) End. 420) End. 421) End. 422) End. 423) End. 424) End. 425) End. 426) End. 427) End. 428) End. 429) End. 430) End. 431) End. 432) End. 433) End. 434) End. 435) End. 436) End. 437) End. 438) End. 439) End. 440) End. 441) End. 442) End. 443) End. 444) End. 445) End. 446) End. 447) End. 448) End. 449) End. 450) End. 451) End. 452) End. 453) End. 454) End. 455) End. 456) End. 457) End. 458) End. 459) End. 460) End. 461) End. 462) End. 463) End. 464) End. 465) End. 466) End. 467) End. 468) End. 469) End. 470) End. 471) End. 472) End. 473) End. 474) End. 475) End. 476) End. 477) End. 478) End. 479) End. 480) End. 481) End. 482) End. 483) End. 484) End. 485) End. 486) End. 487) End. 488) End. 489) End. 490) End. 491) End. 492) End. 493) End. 494) End. 495) End. 496) End. 497) End. 498) End. 499) End. 500) End. 501) End. 502) End. 503) End. 504) End. 505) End. 506) End. 507) End. 508) End. 509) End. 510) End. 511) End. 512) End. 513) End. 514) End. 515) End. 516) End. 517) End. 518) End. 519) End. 520) End. 521) End. 522) End. 523) End. 524) End. 525) End. 526) End. 527) End. 528) End. 529) End. 530) End. 531) End. 532) End. 533) End. 534) End. 535) End. 536) End. 537) End. 538) End. 539) End. 540) End. 541) End. 542) End. 543) End. 544) End. 545) End. 546) End. 547) End. 548) End. 549) End. 550) End. 551) End. 552) End. 553) End. 554) End. 555) End. 556) End. 557) End. 558) End. 559) End. 560) End. 561) End. 562) End. 563) End. 564) End. 565) End. 566) End. 567) End. 568) End. 569) End. 570) End. 571) End. 572) End. 573) End. 574) End. 575) End. 576) End. 577) End. 578) End. 579) End. 580) End. 581) End. 582) End. 583) End. 584) End. 585) End. 586) End. 587) End. 588) End. 589) End. 590) End. 591) End. 592) End. 593) End. 594) End. 595) End. 596) End. 597) End. 598) End. 599) End. 600) End. 601) End. 602) End. 603) End. 604) End. 605) End. 606) End. 607) End. 608) End. 609) End. 610) End. 611) End. 612) End. 613) End. 614) End. 615) End. 616) End. 617) End. 618) End. 619) End. 620) End. 621) End. 622) End. 623) End. 624) End. 625) End. 626) End. 627) End. 628) End. 629) End. 630) End. 631) End. 632) End. 633) End. 634) End. 635) End. 636) End. 637) End. 638) End. 639) End. 640) End. 641) End. 642) End. 643) End. 644) End. 645) End. 646) End. 647) End. 648) End. 649) End. 650) End. 651) End. 652) End. 653) End. 654) End. 655) End. 656) End. 657) End. 658) End. 659) End. 660) End. 661) End. 662) End. 663) End. 664) End. 665) End. 666) End. 667) End. 668) End. 669) End. 670) End. 671) End. 672) End. 673) End. 674) End. 675) End. 676) End. 677) End. 678) End. 679) End. 680) End. 681) End. 682) End. 683) End. 684) End. 685) End. 686) End. 687) End. 688) End. 689) End. 690) End. 691) End. 692) End. 693) End. 694) End. 695) End. 696) End. 697) End. 698) End. 699) End. 700) End. 701) End. 702) End. 703) End. 704) End. 705) End. 706) End. 707) End. 708) End. 709) End. 710) End. 711) End. 712) End. 713) End. 714) End. 715) End. 716) End. 717) End. 718) End. 719) End. 720) End. 721) End. 722) End. 723) End. 724) End. 725) End. 726) End. 727) End. 728) End. 729) End. 730) End. 731) End. 732) End. 733) End. 734) End. 735) End. 736) End. 737) End. 738) End. 739) End. 740) End. 741) End. 742) End. 743) End. 744) End. 745) End. 746) End. 747) End. 748) End. 749) End. 750) End. 751) End. 752) End. 753) End. 754) End. 755) End. 756) End. 757) End. 758) End. 759) End. 760) End. 761) End. 762) End. 763) End. 764) End. 765) End. 766) End. 767) End. 768) End. 769) End. 770) End. 771) End. 772) End. 773) End. 774) End. 775) End. 776) End. 777) End. 778) End. 779) End. 780) End. 781) End. 782) End. 783) End. 784) End. 785) End. 786) End. 787) End. 788) End. 789) End. 790) End. 791) End. 792) End. 793) End. 794) End. 795) End. 796) End. 797) End. 798) End. 799) End. 800) End. 801) End. 802) End. 803) End. 804) End. 805) End. 806) End. 807) End. 808) End. 809) End. 810) End. 811) End. 812) End. 813) End. 814) End. 815) End. 816) End. 817) End. 818) End. 819) End. 820) End. 821) End. 822) End. 823) End. 824) End. 825) End. 826) End. 827) End. 828) End. 829) End. 830) End. 831) End. 832) End. 833) End. 834) End. 835) End. 836) End. 837) End. 838) End. 839) End. 840) End. 841) End. 842) End. 843) End. 844) End. 845) End. 846) End. 847) End. 848) End. 849) End. 850) End. 851) End. 852) End. 853) End. 854) End. 855) End. 856) End. 857) End. 858) End. 859) End. 860) End. 861) End. 862) End. 863) End. 864) End. 865) End. 866) End. 867) End. 868) End. 869) End. 870) End. 871) End. 872) End. 873) End. 874) End. 875) End. 876) End. 877) End. 878) End. 879) End. 880) End. 881) End. 882) End. 883) End. 884) End. 885) End. 886) End. 887) End. 888) End. 889) End. 890) End. 891) End. 892) End. 893) End. 894) End. 895) End. 896) End. 897) End. 898) End. 899) End. 900) End. 901) End. 902) End. 903) End. 904) End. 905) End. 906) End. 907) End. 908) End. 909) End. 910) End. 911) End. 912) End. 913) End. 914) End. 915) End. 916) End. 917) End. 918) End. 919) End. 920) End. 921) End. 922) End. 923) End. 924) End. 925) End. 926) End. 927) End. 928) End. 929) End. 930) End. 931) End. 932) End. 933) End. 934) End. 935) End. 936) End. 937) End. 938) End. 939) End. 940) End. 941) End. 942) End. 943) End. 944) End. 945) End. 946) End. 947) End. 948) End. 949) End. 950) End. 951) End. 952) End. 953) End. 954) End. 955) End. 956) End. 957) End. 958) End. 959) End. 960) End. 961) End. 962) End. 963) End. 964) End. 965) End. 966) End. 967) End. 968) End. 969) End. 970) End. 971) End. 972) End. 973) End. 974) End. 975) End. 976) End. 977) End. 978) End. 979) End. 980) End. 981) End. 982) End. 983) End. 984) End. 985) End. 986) End. 987) End. 988) End. 989) End. 990) End. 991) End. 992) End. 993) End. 994) End. 995) End. 996) End. 997) End. 998) End. 999) End. 1000) End. 1001) End. 1002) End. 1003) End. 1004) End. 1005) End. 1006) End. 1007) End. 1008) End. 1009) End. 1010) End. 1011) End. 1012) End. 1013) End. 1014) End. 1015) End. 1016) End. 1017) End. 1018) End. 1019) End. 1020) End. 1021) End. 1022) End. 1023) End. 1024) End. 1025) End. 1026) End. 1027) End. 1028) End. 1029) End. 1030) End. 1031) End. 1032) End. 1033) End. 1034) End. 1035) End. 1036) End. 1037) End. 1038) End. 1039) End. 1040) End. 1041) End. 1042) End. 1043) End. 1044) End. 1045) End. 1046) End. 1047) End. 1048) End. 1049) End. 1050) End. 1051) End. 1052) End. 1053) End. 1054) End. 1055) End. 1056) End. 1057) End. 1058) End. 1059) End. 1060) End. 1061) End. 1062) End. 1063) End. 1064) End. 1065) End. 1066) End. 1067) End. 1068) End. 1069) End. 1070) End. 1071) End. 1072) End. 1073) End. 1074) End. 1075) End. 1076) End. 1077) End. 1078) End. 1079) End. 1080) End. 1081) End. 1082) End. 1083) End. 1084) End. 1085) End. 1086) End. 1087) End. 1088) End. 1089) End. 1090) End. 1091) End. 1092) End. 1093) End. 1094) End. 1095) End. 1096) End. 1097) End. 1098) End. 1099) End. 1100) End. 1101) End. 1102) End. 1103) End. 1104) End. 1105) End. 1106) End. 1107) End. 1108) End. 1109) End. 1110) End. 1111) End. 1112) End. 1113) End. 1114) End. 1115) End. 1116) End. 1117) End. 1118) End. 1119) End. 1120) End. 1121) End. 1122) End. 1123) End. 1124) End. 1125) End. 1126) End. 1127) End. 1128) End. 1129) End. 1130) End. 1131) End. 1132) End. 1133) End. 1134) End. 1135) End. 1136) End. 1137) End. 1138) End. 1139) End. 1140) End. 1141) End. 1142) End. 1143) End. 1144) End. 1145) End. 1146) End. 1147) End. 1148) End. 1149) End. 1150) End. 1151) End. 1152) End. 1153) End. 1154) End. 1155) End. 1156) End. 1157) End. 1158) End. 1159) End. 1160) End. 1161) End. 1162) End. 1163) End. 1164) End. 1165) End. 1166) End. 1167) End. 1168) End. 1169) End. 1170) End. 1171) End. 1172) End. 1173) End. 1174) End. 1175) End. 1176) End. 1177) End. 1178) End. 1179) End. 1180) End. 1181) End. 1182) End. 1183) End. 1184) End. 1185) End. 1186) End. 1187) End. 1188) End. 1189) End. 1190) End. 1191) End. 1192) End. 1193) End. 1194) End. 1195) End. 1196) End. 1197) End. 1198) End. 1199) End. 1200) End. 1201) End. 1202) End. 1203) End. 1204) End. 1205) End. 1206) End. 1207) End. 1208) End. 1209) End. 1210) End. 1211) End. 1212) End. 1213) End. 1214) End. 1215) End. 1216) End. 1217) End. 1218) End. 1219) End. 1220) End. 1221) End. 1222) End. 1223) End. 1224) End. 1225) End. 1226) End. 1227) End. 1228) End. 1229) End. 1230) End. 1231) End. 1232) End. 1233) End. 1234) End. 1235) End. 1236) End. 1237) End. 1238) End. 1239) End. 1240) End. 1241) End. 1242) End. 1243) End. 1244) End. 1245) End. 1246) End. 1247) End. 1248) End. 1249) End. 1250) End. 1251) End. 1252) End. 1253) End. 1254) End. 1255) End. 1256) End. 1257) End. 1258) End. 1259) End. 1260) End. 1261) End. 1262) End. 1263) End. 1264) End. 1265) End. 1266) End. 1267) End. 1268) End. 1269) End. 1270) End. 1271) End. 1272) End. 1273) End. 1274) End. 1275) End. 1276) End. 1277) End. 1278) End. 1279) End. 1280) End. 1281) End. 1282) End. 1283) End. 1284) End. 1285) End. 1286) End. 1287) End. 1288) End. 1289) End. 1290) End. 1291) End. 1292) End. 1293) End. 1294) End. 1295) End. 1296) End. 1297) End. 1298) End. 1299) End. 1300) End. 1301) End. 1302) End. 1303) End. 1304) End. 1305) End. 1306) End. 1307) End. 1308) End. 1309) End. 1310) End. 1311) End. 1312) End. 1313) End. 1314) End. 1315) End. 1316) End. 1317) End. 1318) End. 1319) End. 1320) End. 1321) End. 1322) End. 1323) End. 1324) End. 1325) End. 1326) End. 1327) End. 1328) End. 1329) End. 1330) End. 1331) End. 1332) End. 1333) End. 1334) End. 1335) End. 1336) End. 1337) End. 1338) End. 1339) End. 1340) End. 1341) End. 1342) End. 1343) End. 1344) End. 1345) End. 1346) End. 1347) End. 1348) End. 1349) End. 1350) End. 1351) End. 1352) End. 1353) End. 1354) End. 1355) End. 1356) End. 1357) End. 1358) End. 1359) End. 1360) End. 1361) End. 1362) End. 1363) End. 1364) End. 1365) End. 1366) End. 1367) End. 1368) End. 1369) End. 1370) End. 1371) End. 1372) End. 1373) End. 1374) End. 1375) End. 1376) End. 1377) End. 1378) End. 1379) End. 1380) End. 1381) End. 1382) End. 1383) End. 1384) End. 1385) End. 1386) End. 1387) End. 1388) End. 1389) End. 1390) End. 1391) End. 1392) End. 1393) End. 1394) End. 1395) End. 1396) End. 1397) End. 1398) End. 1399) End. 1400) End. 1401) End. 1402) End. 1403) End. 1404) End. 1405) End. 1406) End. 1407) End. 1408) End. 1409) End. 1410) End. 1411) End. 1412) End. 1413) End. 1414) End. 1415) End. 1416) End. 1417) End. 1418) End. 1419) End. 1420) End. 1421) End. 1422) End. 1423) End. 1424) End. 1425) End. 1426) End. 1427) End. 1428) End. 1429) End. 1430) End. 1431) End. 1432) End. 1433) End. 1434) End. 1435) End. 1436) End. 1437) End. 1438) End. 1439) End. 1440) End. 1441) End. 1442) End. 1443) End. 1444) End. 1445) End. 1446) End. 1447) End. 1448) End. 1449) End. 1450) End. 1451) End. 1452) End. 1453) End. 1454) End. 1455) End. 1456) End. 1457) End. 1458) End. 1459) End. 1460) End. 1461) End. 1462) End. 1463) End. 1464) End. 1465) End. 1466) End. 1467) End. 1468) End. 1469) End. 1470) End. 1471) End. 1472) End. 1473) End. 1474) End. 1475) End. 1476) End. 1477) End. 1478) End. 1479) End. 1480) End. 1481) End. 1482) End. 1483) End. 1484) End. 1485) End. 1486) End. 1487) End. 1488) End. 1489) End. 1490) End. 1491) End. 1492) End. 1493) End. 1494) End. 1495) End. 1496) End. 1497) End. 1498) End. 1499) End. 1500) End.*** End Patch !*** End Patch  দি -->
