### EcoLens.Api 功能与 API 一览（v0.1）

下表列出当前项目已实现的功能与对应 API（若存在），并标注实现状态与说明。

| 模块 | API | 方法 | 认证/角色 | 实现状态 | 说明 |
| --- | --- | --- | --- | --- | --- |
| Auth | `/api/Auth/register` | POST | 匿名 | 已实现 | 用户注册，写入数据库，返回 JWT。 |
| Auth | `/api/Auth/login` | POST | 匿名 | 已实现 | 用户登录校验密码，封禁用户会被拒绝（`IsActive=false`）。 |
| UserProfile | `/api/user/profile` | GET | 登录 | 已实现 | 返回用户资料与 Rank。 |
| UserProfile | `/api/user/profile` | PUT | 登录 | 已实现 | 使用 DTO 支持更新 Nickname(映射 Username)、AvatarUrl、Region。 |
| UserProfile | `/api/user/change-password` | POST | 登录 | 已实现 | 校验旧密码并更新新密码。 |
| Activity | `/api/Activity/upload` | POST | 登录 | 已实现 | 基于 `Label` 查找 `CarbonReference`，计算排放并保存。当 `Category=Utility` 时，优先匹配用户 `Region` 的碳因子；若无匹配或用户无 `Region`，则退回匹配 `Region` 为空的通用碳因子。未找到匹配的碳参考数据将返回 404。请求为 `[FromForm]` `CreateActivityLogDto` (包含 `Label`, `Category`, `Quantity`)。 |
| Activity | `/api/Activity/dashboard` | GET | 登录 | 已实现 | 返回当前用户的仪表盘数据，包含近 7 天排放趋势、当日碳中和差值（目标 10kg/天）、累计等效植树数（按 `TotalCarbonSaved / 20` 计算）。 |
| Activity | `/api/Activity/my-logs` | GET | 登录 | 已实现 | 获取当前用户活动日志列表，按 `CreatedAt` 倒序排列。 |
| Activity | `/api/Activity/stats` | GET | 登录 | 已实现 | 获取当前用户的统计信息，包括总排放、记录数、折算树木（按 20kg/棵计算）以及全服排名（按 `TotalCarbonSaved` 倒序）。 |
| Activity | `/api/Activity/chart-data` | GET | 登录 | 已实现 | 获取过去 `N` 天（默认 7 天）的每日碳排放总量数据。请求参数 `days` (int, 默认 7, 必须 > 0)。缺失日期数据补 0。 |
| Activity | `/api/Activity/heatmap` | GET | 登录 | 已实现 | 按区域 (`Region`) 统计用户的总碳减排量，用于热力图展示，响应按 `TotalSaved` 倒序。 |
| Step | `/api/Step/sync` | POST | 登录 | 已实现 | 同步计步数据，并计算碳抵消和积分。请求为 `[FromBody]` `SyncStepsRequestDto` (包含 `StepCount` (int), `Date` (DateTime))。计步数据 `StepCount` 必须非负。碳抵消 (`CarbonOffset`) 计算规则: 每 1000 步 = 0.1 kg CO2。积分 (`CurrentPoints`) 计算规则: `CarbonOffset * 100`，结果四舍五入取整。若当日已存在记录，则更新 `StepCount` 和 `CarbonOffset`，并按差值调整用户 `TotalCarbonSaved` 和 `CurrentPoints`；若当日无记录，则创建新记录并增加。 |
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
| Leaderboard | `/api/Leaderboard/top-users` | GET | 登录 | 已实现 | 获取总碳减排量 (`TotalCarbonSaved`) 排行榜前 10 名用户。 |
| Leaderboard | `/api/Leaderboard/follow/{targetUserId}` | POST | 登录 | 已实现 | 关注指定用户。请求参数 `targetUserId` (int)。不能关注自己。重复关注将返回 204 (NoContent) 或 200 (Ok)。若 `targetUserId` 不存在，返回 404。 |
| Leaderboard | `/api/Leaderboard/friends` | GET | 登录 | 已实现 | 获取当前用户关注的好友列表，按总碳减排量 (`TotalCarbonSaved`) 排序。 |
| AiChat | `/api/ai/chat` | POST | 登录 | Mock/示例 | 关键词触发示例回复（预留对接大模型 TODO）。 |
| Insight | `/api/Insight/weekly-report` | GET | 登录 | Mock/示例 | 基于近 7 天日志返回示例洞见（预留外部服务 TODO）。 |
| Vision | `/api/Vision/analyze` | POST | 登录 | Mock/示例 | 读取上传图片文件名简单识别（预留 Python 微服务对接 TODO）。 |

### 基础设施与实体（非 API）
- 用户实体：新增 `IsActive`（默认 true）、`Region` 字段；被封禁用户无法登录。
- 碳因子实体：新增可选 `Region` 字段；在 DbContext 上为 `(LabelName, Category, Region)` 建立唯一索引。
- 步数实体：`StepRecord` 支持每日一条的更新/新增与减排换算。

### 备注
- Admin 严格权限控制在 `AdminController` 中通过 `[Authorize(Roles = "Admin")]` 实现；`CarbonFactorController` 的 `/api/admin/factor` 目前仅 `[Authorize]`，如需收敛为 AdminOnly 可按需调整。 +
