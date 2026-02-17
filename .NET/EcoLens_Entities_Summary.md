# EcoLens 实体与属性摘要

| Entity Name | Property | Type | Description |
| :--- | :--- | :--- | :--- |
| BaseEntity | Id | int | 主键标识。 |
| BaseEntity | CreatedAt | DateTime | 创建时间（UTC）。 |
| BaseEntity | UpdatedAt | DateTime | 最后更新时间（UTC）。 |
| ApplicationUser | Username | string | 用户名（用于登录/展示）。 |
| ApplicationUser | Email | string | 用户邮箱。 |
| ApplicationUser | PasswordHash | string | 密码哈希（安全存储）。 |
| ApplicationUser | Role | UserRole | 用户角色（User/Admin）。 |
| ApplicationUser | AvatarUrl | string? | 头像图片地址。 |
| ApplicationUser | TotalCarbonSaved | decimal(18,2) | 累计减排量，用于排行榜/进度。 |
| ApplicationUser | CurrentPoints | int | 当前积分。 |
| ApplicationUser | ActivityLogs | ICollection<ActivityLog> | 该用户的活动日志集合。 |
| ApplicationUser | AiInsights | ICollection<AiInsight> | 该用户收到的 AI 洞见集合。 |
| ApplicationUser | StepRecords | ICollection<StepRecord> | 该用户的步数记录集合。 |
| CarbonReference | LabelName | string | 标签名，如 “Beef”。 |
| CarbonReference | Category | CarbonCategory | 参考类别（Food/Transport）。 |
| CarbonReference | Co2Factor | decimal(18,4) | 每单位的 CO₂ 系数。 |
| CarbonReference | Unit | string | 计量单位（如 kg、km）。 |
| ActivityLog | UserId | int | 外键：所属用户 Id。 |
| ActivityLog | CarbonReferenceId | int | 外键：所用碳参考 Id。 |
| ActivityLog | Quantity | decimal(18,4) | 活动数量（按参考的 Unit）。 |
| ActivityLog | TotalEmission | decimal(18,4) | 活动总排放量（Quantity×Co2Factor）。 |
| ActivityLog | ImageUrl | string? | 佐证图片 URL（可选）。 |
| ActivityLog | DetectedLabel | string? | 识别出的标签/物品名（可选）。 |
| ActivityLog | User | ApplicationUser? | 导航到所属用户。 |
| ActivityLog | CarbonReference | CarbonReference? | 导航到所用碳参考。 |
| AiInsight | UserId | int | 外键：所属用户 Id。 |
| AiInsight | Content | string (≤5000) | AI 洞见的文本内容。 |
| AiInsight | Type | InsightType | 洞见类型（周报/日常提示）。 |
| AiInsight | IsRead | bool | 是否已读。 |
| AiInsight | User | ApplicationUser? | 导航到所属用户。 |
| StepRecord | UserId | int | 外键：所属用户 Id。 |
| StepRecord | StepCount | int | 当日步数。 |
| StepRecord | RecordDate | DateTime | 记录日期。 |
| StepRecord | CarbonOffset | decimal(18,4) | 按步数折抵的碳减排量。 |
| StepRecord | User | ApplicationUser? | 导航到所属用户。 |

## Enums

- UserRole
  - User = 0
  - Admin = 1

- CarbonCategory
  - Food = 0
  - Transport = 1

- InsightType
  - WeeklyReport = 0
  - DailyTip = 1

