### EcoLens 客户版功能说明（ReadMe_customer.md）

本文档面向**客户/验收**，汇总本仓库当前版本已实现的**软件功能清单**，并按类型分类说明（Web、Android、后端 API、AI/识别服务与运维监控）。

---

### 1. 产品与系统组成

- **Web 前端（用户端）**：`web/`（React + Vite），用于登录/记录/看板/排行榜/种树/AI 助手等。
- **Web 前端（管理端）**：`web/`（Admin Portal），用于用户管理、排放因子维护、数据分析与系统维护。
- **后端 API**：`.NET/EcoLens.Api`（.NET 8 Web API + Swagger + JWT + SQL Server）。
- **AI 识别服务（食物图像识别）**：`VisionService/`（FastAPI），对食物图片进行识别并返回标签/置信度。
- **监控与可观测性**：`monitoring/`（Prometheus 抓取配置），并接入 Application Insights（遥测）。

---

### 2. 功能总览（按类型分类）

#### 2.1 账号与身份认证（普通用户/管理员）

- **用户注册**
  - 注册账号并返回 JWT Token
  - 具备**用户名敏感词过滤**（不当内容拒绝注册）
- **用户登录**
  - 登录成功返回 JWT Token
  - 支持**封禁用户拒绝登录**
- **管理员登录**
  - 独立管理员登录入口（用于管理后台）
- **JWT 身份认证**
  - 除注册/登录等少量接口外，业务接口均要求在请求头携带：`Authorization: Bearer <token>`

#### 2.2 个人资料与账号安全（我的资料）

- **查看/编辑个人资料**
  - 昵称、地区（Location/Region）、生日、邮箱等
- **头像管理**
  - 上传头像（图片）并在各处显示
  - 支持通过后端接口获取头像图片资源（后端可缓存）
- **修改密码 / 校验旧密码**
  - 修改密码前可先校验旧密码正确性（移动端采用分步验证）

#### 2.3 碳排放记录与计算（核心业务）

- **首页看板（本月汇总）**
  - 本月总排放
  - 分类别：食物 / 出行 / 水电
- **活动记录（Activity Logs）**
  - 上传活动记录（标签 + 数量 + 单位 + 类别），自动计算排放并保存
  - Utility 类别支持按用户地区优先匹配因子
  - 当本地无因子时，支持通过 **Climatiq** 获取估算并缓存为本地因子
  - 看板与统计：近 7 天趋势、图表数据、区域热力图等
- **食物（Food Records）**
  - 食物名称/份量计算排放（支持 g/kg/portion 等单位换算）
  - 食物图片识别（调用 AI 识别服务），识别后计算排放
  - 一站式“识别 + 计算 + 入库”流程
  - 食物记录列表/详情/删除（支持分页与日期筛选）
- **出行（Travel Logs）**
  - 路线预览（不落库）：距离、时长、polyline、预估排放
  - 创建出行记录（落库）：路线、距离、时长、排放，并支持备注
  - 出行记录列表（分页/筛选）、详情、删除
  - 出行统计（总记录数、总距离、总排放、按出行方式分组）
  - 集成 **Google Maps API（地理编码/路径规划）**
- **水电账单（Utility Bills）**
  - 上传账单文件（图片/PDF）→ OCR 识别 → 数据提取 → 排放计算（可先不保存，供用户确认）
  - 手动创建/保存账单（确认后落库）
  - 重复账单检测（避免重复录入）
  - 账单列表（分页/筛选）、详情、删除
  - 账单统计（用量、排放、按类型统计）
  - 账单 OCR 依赖：**Google Cloud Vision API**（OCR）与账单解析/计算逻辑
- **饮食记录（Diet）与饮食模板（Diet Templates）**
  - 饮食记录：创建、分页查询、详情、删除
  - 饮食模板：创建、查询列表（用于复用/推荐的饮食方案）
- **统一记录管理（Records）**
  - 移动端支持按类型（Food/Transport/Utilities）与日期范围筛选记录
  - 支持批量删除（按类型删除指定记录，并触发汇总/积分重算）

#### 2.4 激励体系：步数、积分与种树

- **步数同步（计步数据）**
  - Android 端本地计步后同步到后端（按日期）
  - 按规则将步数折算为碳抵消与积分，并记录积分来源（PointAwardLogs）
- **种树（Trees）**
  - 获取当前树状态（树数量、当前成长进度、今日步数、可用步数）
  - 投入步数换算为成长进度（约 150 步 = 1% 进度，15000 步 = 1 棵树成熟）
  - 树成熟后累计树数量，并可触发**种树积分奖励**
- **积分规则（Points）**
  - 每日达标奖励：当日同时有食物记录与出行记录，且排放低于基准值时，可获得积分与连续天数奖励
  - 周期奖励：连续达标每 7 天游额外奖励
  - 种树奖励：每棵树固定奖励积分
- **排行榜（Leaderboard）**
  - 今日/周/月/总榜（基于积分与排放聚合）
  - Top10（按总碳减排）
  - 关注用户与好友榜（Friends）

#### 2.5 社区与互动

- **社区帖子（Posts）**
  - 帖子列表（分页，可按类型 User/Official）
  - 帖子详情（含评论列表；浏览量自增）
  - 发布帖子（可带图片 URL 列表）
  - 评论帖子
- **管理端内容治理**
  - 管理员可删除帖子

#### 2.6 AI 助手与识别能力

- **AI 助手聊天（AiChat / Consultant）**
  - 用户可与 AI 进行问答，获取低碳建议
  - 支持“碳排放分析简报”：按近 7 天/近 30 天聚合食物/出行/水电排放，生成精简建议
  - AI 接口适配：支持 OpenAI 兼容 `chat/completions` 形态，并可切换到 Gemini 原生形态
- **食物图片识别（VisionService + Vision API）**
  - Python 识别服务采用“双模型融合策略”，输出标签、置信度与来源模型
  - 后端提供统一入口将图片转发到识别服务并返回结果
- **条形码 → 商品信息 → 碳因子（Barcode / Product）**
  - Android 端可本地扫描条形码（ZXing）
  - 后端优先使用本地缓存，缺失时调用 **OpenFoodFacts** 拉取商品信息并缓存
  - 若 OpenFoodFacts 缺少有效 CO₂ 数据，可使用 **Climatiq** 作为后备估算或回退默认值

#### 2.7 媒体与文件

- **图片上传**
  - 上传图片到后端静态资源目录并返回可访问 URL（用于头像、帖子图片等）

#### 2.8 管理后台（Admin Portal）

管理后台需要管理员身份（`Admin` 角色）：

- **数据总览**
  - 区域减排统计（总览/周/月）
  - 周影响（最近多周趋势）
  - 分类占比、活跃度/参与度分析等
- **排放因子与参考数据管理**
  - 碳因子/参考数据列表（支持筛选）
  - 新增/更新/删除
  - 支持批量导入（JSON）
- **用户管理**
  - 用户列表
  - 封禁/解封用户（禁止登录与参与）
  - 批量更新用户信息
  - 查看指定用户排放统计
  - 删除指定用户数据（合规/清理场景）
- **内容治理**
  - 删除帖子
- **系统设置与数据库维护**
  - 获取/更新系统设置
  - 数据库统计
  - 清空数据库（高风险操作，管理端）

#### 2.9 系统运维与监控

- **Swagger API 文档**
  - 后端开发/联调可通过 Swagger UI 查看与调用接口
- **指标监控**
  - 后端与 AI 识别服务均支持 Prometheus 指标抓取（`/metrics`）
  - `monitoring/prometheus.yml` 提供抓取配置示例（含 Azure 部署目标）
- **遥测**
  - 接入 Application Insights（请求/依赖/异常等遥测）
- **一键启动脚本（开发环境）**
  - `start-all.ps1`：启动后端与前端（并检查本地 VisionService 是否运行）
  - `start-backend.ps1`、`start-frontend.ps1`：分别启动

---

### 3. 多端功能矩阵（客户视角）

说明：下表从“用户能做什么”出发，标注主要承载端（Web/Android），并给出后端支撑能力。

| 功能 | Web 用户端 | Android App | 后端 API | 备注 |
|---|---|---|---|---|
| 注册/登录 | ✅ | ✅ | ✅ | JWT 登录态 |
| 个人资料编辑（昵称/地区/生日/邮箱） | ✅ | ✅ | ✅ | 支持头像 |
| 修改密码（含旧密码校验） | ✅ | ✅ | ✅ | Android 分步校验 |
| 食物记录：图片识别 → 计算 → 保存 | ✅ | ✅ | ✅ | Android 支持条码优先 |
| 食物记录：列表/筛选/删除 | ✅ | ✅ | ✅ | 支持分页/时间筛选 |
| 出行记录：预览路线与排放 | ✅ |（页面侧更偏录入）✅ | ✅ | Google Maps |
| 出行记录：创建/列表/详情/删除/统计 | ✅ | ✅ | ✅ | polyline 可绘制路线 |
| 水电账单：上传识别（OCR） | ✅ | ✅ | ✅ | 图片/PDF，支持不保存预览 |
| 水电账单：手动保存/列表/详情/删除/统计 | ✅ | ✅ | ✅ | 重复账单检测 |
| 活动记录：上传与统计 | ✅（页面/接口具备） |（可扩展） | ✅ | 支持 Climatiq 兜底 |
| 记录中心：按类型/日期筛选 | ✅ | ✅ | ✅ | Android 支持批量删除 |
| 记录中心：批量删除（按类型） | ✅（可对接） | ✅ | ✅ | 删除后重算汇总/积分 |
| 积分与连续奖励 | ✅ | ✅ | ✅ | 积分来源可追踪 |
| 计步同步 |（可扩展） | ✅ | ✅ | 计步抵消 + 积分 |
| 种树玩法（步数换成长/种树） | ✅ | ✅ | ✅ | 树成熟奖励积分 |
| 排行榜（今日/周/月/总、好友榜） | ✅ | ✅ | ✅ | 关注关系 |
| 社区（帖子/评论/浏览） | ✅ |（可扩展） | ✅ | 管理端可删帖 |
| AI 助手聊天 | ✅ | ✅ | ✅ | 支持排放分析简报 |
| 管理后台（用户/因子/统计/维护） | ✅ | ❌ | ✅ | 需管理员角色 |

---

### 4. Web 前端功能入口（路由级功能）

#### 4.1 Web 用户端（登录后）

对应 `web/src/App.tsx` 中的主要页面路由：

- **Dashboard**：`/dashboard`（看板/汇总）
- **AI Assistant**：`/ai-assistant`
- **Leaderboard**：`/leaderboard`
- **Records**：`/records`（记录中心）
- **Log Meal**：`/log-meal`（记录食物）
- **Log Travel**：`/log-travel`（记录出行）
- **Log Utility**：`/log-utility`（记录水电）
- **Tree Planting**：`/tree-planting`（种树）
- **Profile**：`/profile`（个人资料）
- **About Me**：`/about-me`（个人对比与月度趋势）

#### 4.2 Web 管理端（Admin Portal）

- **管理员登录**：`/admin/login`
- **管理首页**：`/admin`
- **用户管理**：`/admin/users`
- **排放因子管理**：`/admin/emission-factors`
- **社区分析**：`/admin/community-analytics`
- **用户排放分析**：`/admin/user-emissions`

---

### 5. Android App 功能入口（Activity 级功能）

Android 工程位于 `mobile/AD/`。主要页面包括：

- **引导/启动**
  - `SplashActivity`：启动页
  - `IntroActivity`：引导页
- **账号**
  - `LoginActivity`：登录
  - `RegisterActivity`：注册
- **主页与看板**
  - `MainActivity`：主页（看板 + 今日榜 Top3 + 计步同步）
- **记录录入**
  - `AddFoodActivity`：食物录入（本地条码识别 → 后端条码查询；或图片 → AI 识别）
  - `AddTravelActivity`：出行录入（地图/地点搜索/交通方式）
  - `AddUtilityActivity`：水电录入（账单图片上传识别 + 手动保存）
- **记录中心**
  - `EmissionRecordsActivity`：记录列表（类型/日期筛选、批量删除）
- **互动与激励**
  - `LeaderboardActivity`：排行榜
  - `AiAssistantActivity`：AI 助手聊天
  - `TreePlantingActivity`：种树（把可用步数换算为成长与种树）
- **个人中心**
  - `ProfileActivity`：个人资料、头像上传、改密、历史记录入口、退出登录
  - `ProfileStatsActivity`：统计/趋势（按实现）

---

### 6. 第三方服务与数据源（功能依赖）

- **数据库**：SQL Server（后端使用 EF Core）
- **地图与路线**：Google Maps API（地理编码、路线/距离计算等）
- **食物条码商品库**：OpenFoodFacts API（按条码查商品与 eco score）
- **排放因子后备**：Climatiq API（当本地缺少因子时）
- **账单 OCR**：Google Cloud Vision API（识别文本）+ 后端解析/计算
- **AI 对话/图像理解**：通过 `AiSettings.BaseUrl` 接入（OpenAI 兼容或 Gemini 原生）
- **AI 食物图像识别**：Python VisionService（本地或 Azure 部署）
- **监控**：Prometheus 抓取 `/metrics`；Application Insights 遥测

---

### 7. 权限与数据范围（验收口径）

- **普通用户（User）**：仅可访问/管理自己的记录与资料（食物/出行/水电/步数/树/社区发帖与评论等）。
- **管理员（Admin）**：可访问管理后台能力（用户管理、内容治理、因子维护、统计分析、数据库维护等）。
- **匿名访问（AllowAnonymous）**：少量接口允许匿名（例如部分 AI 聊天入口、头像获取等，具体以接口为准）。

---

### 8. 附录A：后端 API 功能清单（按模块完整列出）

> 说明：以下内容由工具从后端 `Controllers` 路由特性自动提取，用于确保“功能清单完整覆盖”。  
> 注意：ASP.NET 路由通常对大小写不敏感；本清单保留代码中生成的原始形式。  

### 附录A：后端 API 功能清单（自动从 Controller 路由提取）

说明：本附录用于“完整覆盖”，每一行代表一个后端接口路由。`IsAlias=true` 表示该路由是为前端兼容/历史原因提供的**别名路径**。

#### 模块：About

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/about-me` |  |

#### 模块：Activity

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/Activity/chart-data` |  |
| GET | `/api/Activity/daily-net-value` |  |
| GET | `/api/Activity/dashboard` |  |
| GET | `/api/Activity/heatmap` |  |
| GET | `/api/Activity/my-logs` |  |
| GET | `/api/Activity/stats` |  |
| POST | `/api/Activity/upload` |  |

#### 模块：Admin

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/admin/analytics/category-share` |  |
| GET | `/api/admin/analytics/engagement` |  |
| GET | `/api/admin/carbon-reference` |  |
| POST | `/api/admin/carbon-reference` |  |
| DELETE | `/api/admin/carbon-reference/{id:int}` |  |
| POST | `/api/admin/database/clear-all` |  |
| GET | `/api/admin/database/statistics` |  |
| GET | `/api/admin/emission-factors` |  |
| POST | `/api/admin/emission-factors` |  |
| DELETE | `/api/admin/emission-factors/{id}` |  |
| PUT | `/api/admin/emission-factors/{id}` |  |
| POST | `/api/admin/emission-factors/import` |  |
| GET | `/api/admin/impact/weekly` |  |
| DELETE | `/api/admin/posts/{id:int}` |  |
| GET | `/api/admin/regions/stats` |  |
| GET | `/api/admin/regions/stats/monthly` |  |
| GET | `/api/admin/regions/stats/weekly` |  |
| GET | `/api/admin/settings` |  |
| PUT | `/api/admin/settings` |  |
| GET | `/api/admin/users` |  |
| POST | `/api/admin/users/{id:int}/ban` |  |
| GET | `/api/admin/users/{id:int}/emissions/stats` |  |
| DELETE | `/api/admin/users/{userId:int}/data` |  |
| PATCH | `/api/admin/users/batch` |  |
| PUT | `/api/admin/users/batch` |  |
| POST | `/api/admin/users/batch-update` |  |
| GET | `/api/regions/stats` | 别名路由（兼容/对齐） |

#### 模块：AdminAuth

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/admin/auth/login` |  |
| POST | `/api/admin/login` | 别名路由（兼容/对齐） |

#### 模块：AiChat

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/ai/analysis` |  |
| POST | `/api/ai/chat` |  |
| POST | `/api/assistant/chat` | 别名路由（兼容/对齐） |

#### 模块：Auth

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/Auth/login` |  |
| POST | `/api/Auth/register` |  |

#### 模块：Barcode

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/Barcode` |  |
| DELETE | `/api/Barcode/{barcode}` |  |
| GET | `/api/Barcode/{barcode}` |  |

#### 模块：CarbonEmission

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/carbon-emission/batch-delete-typed` |  |

#### 模块：CarbonFactor

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/admin/factor` |  |
| GET | `/api/carbon/factors` |  |
| GET | `/api/carbon/lookup` |  |

#### 模块：Community

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/Community/posts` |  |
| POST | `/api/Community/posts` |  |
| GET | `/api/Community/posts/{id:int}` |  |
| POST | `/api/Community/posts/{id:int}/comments` |  |

#### 模块：Consultant

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/consultant/chat` |  |

#### 模块：Diet

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/Diet` |  |
| DELETE | `/api/Diet/{id}` |  |
| GET | `/api/Diet/{id}` |  |
| GET | `/api/Diet/my-diets` |  |

#### 模块：DietTemplate

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/diet/templates` |  |
| POST | `/api/diet/templates` |  |

#### 模块：Food

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/Food/calculate` |  |
| POST | `/api/Food/calculate-from-image` |  |
| POST | `/api/Food/calculate-simple` |  |
| POST | `/api/Food/ingest-by-name` |  |
| POST | `/api/Food/ingest-from-image` |  |
| POST | `/api/Food/recognize` |  |

#### 模块：FoodRecords

| 方法 | 路径 | 备注 |
|---|---|---|
| DELETE | `/api/FoodRecords/{id}` |  |
| GET | `/api/FoodRecords/{id}` |  |
| GET | `/api/FoodRecords/my-records` |  |

#### 模块：Insight

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/Insight/weekly-report` |  |

#### 模块：Leaderboard

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/Leaderboard` |  |
| GET | `/api/Leaderboard/{username}` |  |
| POST | `/api/Leaderboard/follow/{targetUserId:int}` |  |
| GET | `/api/Leaderboard/friends` |  |
| GET | `/api/Leaderboard/month` |  |
| GET | `/api/Leaderboard/today` |  |
| GET | `/api/Leaderboard/top-users` |  |

#### 模块：MainPage

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/mainpage` |  |

#### 模块：Me

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/me` |  |
| PUT | `/api/me` |  |
| PUT | `/api/me/avatar` |  |
| PUT | `/api/me/password` |  |
| PUT | `/api/user/avatar` | 别名路由（兼容/对齐） |
| GET | `/api/user/me` | 别名路由（兼容/对齐） |
| PUT | `/api/user/me` | 别名路由（兼容/对齐） |

#### 模块：Media

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/Media/upload` |  |

#### 模块：Product

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/Product/{barcode}` |  |

#### 模块：SimpleFood

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/addFood` |  |
| POST | `/api/calculateFood` |  |
| POST | `/api/updateFood` |  |

#### 模块：Step

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/Step/sync` |  |

#### 模块：Travel

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/records/travel` | 别名路由（兼容/对齐） |
| POST | `/api/Travel` |  |
| DELETE | `/api/Travel/{id}` |  |
| GET | `/api/Travel/{id}` |  |
| GET | `/api/Travel/my-travels` |  |
| POST | `/api/Travel/preview` |  |
| GET | `/api/Travel/statistics` |  |

#### 模块：Trees

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/getTree` | 别名路由（兼容/对齐） |
| POST | `/api/postTree` | 别名路由（兼容/对齐） |
| POST | `/api/trees/convert-steps` |  |
| POST | `/api/trees/grow` |  |
| GET | `/api/trees/state` |  |
| PUT | `/api/trees/state` |  |
| GET | `/api/trees/stats` |  |

#### 模块：Trip

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/travel/route/calculate` | 别名路由（兼容/对齐） |
| POST | `/api/Trip/calculate` |  |

#### 模块：UserProfile

| 方法 | 路径 | 备注 |
|---|---|---|
| GET | `/api/user/{userId}/avatar` |  |
| POST | `/api/user/change-password` |  |
| GET | `/api/user/profile` |  |
| PUT | `/api/user/profile` |  |
| POST | `/api/user/verify-password` |  |

#### 模块：Utility

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/records/utilities` | 别名路由（兼容/对齐） |
| GET | `/api/Utility/my-records` |  |
| POST | `/api/Utility/ocr` |  |
| POST | `/api/Utility/record` |  |
| POST | `/api/vision/utility-ocr` | 别名路由（兼容/对齐） |

#### 模块：UtilityBill

| 方法 | 路径 | 备注 |
|---|---|---|
| DELETE | `/api/UtilityBill/{id}` |  |
| GET | `/api/UtilityBill/{id}` |  |
| GET | `/api/UtilityBill/{id}/debug` |  |
| POST | `/api/UtilityBill/manual` |  |
| GET | `/api/UtilityBill/my-bills` |  |
| GET | `/api/UtilityBill/statistics` |  |
| POST | `/api/UtilityBill/upload` |  |
| POST | `/api/vision/utility-bill` | 别名路由（兼容/对齐） |

#### 模块：Vision

| 方法 | 路径 | 备注 |
|---|---|---|
| POST | `/api/Vision/analyze` |  |

