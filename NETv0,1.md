### EcoLens.Api 功能与 API 一览（v0.1）

下表列出当前项目已实现的功能与对应 API（若存在），并标注实现状态与说明。

| 模块 | API | 方法 | 认证/角色 | 实现状态 | 说明 |
| --- | --- | --- | --- | --- | --- |
| Auth | `/api/Auth/register` | POST | 匿名 | 已实现 | 用户注册，写入数据库，返回 JWT。 |
| Auth | `/api/Auth/login` | POST | 匿名 | 已实现 | 用户登录校验密码，封禁用户会被拒绝（`IsActive=false`）。 |
| UserProfile | `/api/user/profile` | GET | 登录 | 已实现 | 返回用户资料与 Rank。 |
| UserProfile | `/api/user/profile` | PUT | 登录 | 已实现 | 使用 DTO 支持更新 Nickname(映射 Username)、AvatarUrl、Region。 |
| UserProfile | `/api/user/change-password` | POST | 登录 | 已实现 | 校验旧密码并更新新密码。 |
| Activity | `/api/Activity/upload` | POST | 登录 | 已实现 | 基于 Label 查找 `CarbonReference`，计算排放并保存；当 `Category=Utility` 时按 User.Region 优先匹配因子，否则退回默认。 |
| Activity | `/api/Activity/dashboard` | GET | 登录 | 已实现 | 返回近 7 天趋势、当日 NeutralityGap(目标10kg/天)、累计等效植树数(TotalSaved/20)。 |
| Activity | `/api/Activity/my-logs` | GET | 登录 | 已实现 | 当前用户活动日志列表。 |
| Activity | `/api/Activity/stats` | GET | 登录 | 已实现 | 总排放、记录数、折算树木、全服排名。 |
| Activity | `/api/Activity/chart-data` | GET | 登录 | 已实现 | 过去 N 天(默认7)每日排放折线数据（补齐缺失日期）。 |
| Activity | `/api/Activity/heatmap` | GET | 登录 | 已实现 | 按用户 Region 汇总 `TotalCarbonSaved`。 |
| Step | `/api/Step/sync` | POST | 登录 | 已实现 | 步数同步：同日有则更新，否则新增；每 1000 步 = 0.1kg 减排，自动累计到 `TotalCarbonSaved` 与 `CurrentPoints`。 |
| Admin | `/api/admin/carbon-reference` | GET | Admin | 已实现 | 按 Category/Region/Label 过滤列出碳因子。 |
| Admin | `/api/admin/carbon-reference` | POST | Admin | 已实现 | 碳因子新增/更新（含可选 Region）。 |
| Admin | `/api/admin/carbon-reference/{id}` | DELETE | Admin | 已实现 | 删除碳因子。 |
| Admin | `/api/admin/users/{id}/ban` | POST | Admin | 已实现 | 封禁/解封用户（切换 `IsActive`）。 |
| CarbonFactor | `/api/carbon/factors` | GET | 登录 | 已实现 | 公共查询碳因子，支持按 Category 过滤。 |
| CarbonFactor | `/api/admin/factor` | POST | 登录 | 已实现 | 因子 Upsert（示例接口，当前仅登录限制，非严格 AdminOnly）。 |
| Community | `/api/Community/posts` | GET | 登录 | 已实现 | 分页获取帖子列表，可按 `type=User/Official` 过滤。 |
| Community | `/api/Community/posts/{id}` | GET | 登录 | 已实现 | 获取帖子详情及评论，并自增浏览量。 |
| Community | `/api/Community/posts` | POST | 登录 | 已实现 | 发帖（Title/Content 必填，支持 ImageUrls、Type）。 |
| Community | `/api/Community/posts/{id}/comments` | POST | 登录 | 已实现 | 评论帖子。 |
| Leaderboard | `/api/Leaderboard/top-users` | GET | 登录 | 已实现 | 按 `TotalCarbonSaved` 排名前 10。 |
| Leaderboard | `/api/Leaderboard/follow/{targetUserId}` | POST | 登录 | 已实现 | 关注用户（防重复、不可关注自己）。 |
| Leaderboard | `/api/Leaderboard/friends` | GET | 登录 | 已实现 | 我关注的人排行榜（按 `TotalCarbonSaved` 排序）。 |
| AiChat | `/api/ai/chat` | POST | 登录 | 已实现 | 调用 OpenAI 兼容接口（BaseUrl）转发至 Gemini 模型返回回复。 |
| Insight | `/api/Insight/weekly-report` | GET | 登录 | Mock/示例 | 基于近 7 天日志返回示例洞见（预留外部服务 TODO）。 |
| Vision | `/api/Vision/analyze` | POST | 登录 | Mock/示例 | 读取上传图片文件名简单识别（预留 Python 微服务对接 TODO）。 |
| Product | `/api/product/{barcode}` | GET | 登录 | 已实现 | 通过条形码在 `CarbonReferences` 中查询商品/因子，返回名称、因子与单位（PB-005）。 |
| Trip | `/api/trip/calculate` | POST | 登录 | Mock/示例 | 随机 1-50km 距离，根据 `TransportMode` 估算排放（注释注明应调用 Google Maps Routes API）（PB-005）。 |
| Media | `/api/media/upload` | POST | 登录 | 已实现 | 上传图片至 `wwwroot/uploads/{yyyyMMdd}/{guid}.ext`，返回相对 URL（PB-008/PB-013）。 |
| Admin | `/api/admin/posts/{id}` | DELETE | Admin | 已实现 | 软删除帖子（设置 `IsDeleted=true`）（PB-008）。 |
| Utility | `/api/Utility/ocr` | POST | 登录 | Mock/示例 | 模拟返回 `{ electricityUsage, waterUsage, gasUsage }`（注释注明应接入 Azure Form Recognizer/Google ML Kit）（PB-013）。 |
| Consultant | `/api/consultant/chat` | POST | 登录 | 已实现 | AI 智能顾问：接收 `{question}`，经 OpenAI Chat Completions 调用 Gemini 返回 `{answer}`。 |

### 基础设施与实体（非 API）
- 用户实体：新增 `IsActive`（默认 true）、`Region` 字段；被封禁用户无法登录。
- 碳因子实体：新增可选 `Region` 字段；在 DbContext 上为 `(LabelName, Category, Region)` 建立唯一索引。
- 步数实体：`StepRecord` 支持每日一条的更新/新增与减排换算。
- PB-005：在 `CarbonReference` 新增可选 `Barcode` 字段并建立索引；新增 `ProductController` 与 `TripController`（Mock）。
- PB-008：在 `Post` 实体新增 `IsDeleted` 并在 `DbContext` 添加全局过滤；在 `AdminController` 增加软删除帖子接口。
- PB-008/PB-013：新增 `MediaController` 支持图片上传；`Program.cs` 启用 `UseStaticFiles()` 用于访问 `/uploads/...`。
- PB-013：新增 `UtilityController` 的 OCR Mock 接口，预留对接 Azure Form Recognizer/Google ML Kit。

### 备注
- Admin 严格权限控制在 `AdminController` 中通过 `[Authorize(Roles = "Admin")]` 实现；`CarbonFactorController` 的 `/api/admin/factor` 目前仅 `[Authorize]`，如需收敛为 AdminOnly 可按需调整。 +
 - 已集成 AI 智能顾问：`AiSettings` 配置（`ApiKey`, `BaseUrl`, `Model`）存于 `appsettings.Development.json`；服务通过 OpenAI Chat Completions 协议调用 Gemini（中转地址 `BaseUrl`）。`/api/ai/chat` 与 `/api/consultant/chat` 均使用该服务。

