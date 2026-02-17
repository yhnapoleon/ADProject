# API 需求清单

> 说明：已遍历 `mobile/AD/app/src/main/java` 下各页面（如 `LoginActivity`, `RegisterActivity`, `AddFoodActivity`, `AddTravelActivity`, `AddUtilityActivity`, `EmissionRecordsActivity`, `ProfileActivity`, `ProfileStatsActivity`, `LeaderboardActivity`, `AiAssistantActivity`, `TreePlantingActivity`），当前未发现实际的网络请求实现（未接入 Retrofit/OkHttp 等）。以下清单依据页面展示的数据结构与交互推导出后端 API 需求，供后端核对与移动端后续对接。

| 模块/功能 | API 路径 | 方法 | 请求参数 (Input) | 期望响应字段 (Output) | 前端调用位置 (可选) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 用户登录 | `/api/auth/login` | POST | `email`(string, 必填) 或 `username`(string, 选填), `password`(string, 必填) | `token`(string), `user.id`(string), `user.username`(string), `user.nickname`(string), `user.email`(string), `user.location`(string), `user.avatar`(string, url) | `LoginActivity.kt` |
| 用户注册 | `/api/auth/register` | POST | `username`(string, 必填), `email`(string, 必填), `password`(string, 必填), `dateOfBirth`(string, YYYY-MM-DD, 必填), `location`(string, 必填) | `user.id`(string), `user.username`(string), `user.email`(string), 可选: `token`(string) | `RegisterActivity.kt` |
| 获取当前用户信息 | `/api/me` | GET | 需携带 `Authorization: Bearer <token>` | `id`, `username`, `nickname`, `email`, `location`, `avatar`, `joinDate`, `pointsWeek`, `pointsMonth`, `pointsTotal` | `ProfileActivity.kt` |
| 更新个人资料 | `/api/me` | PUT | 需授权; `nickname?`(string), `email?`(string), `location?`(string), `password?`(string) | 同获取用户信息字段（更新后的值） | `ProfileActivity.kt` |
| 更新头像 | `/api/me/avatar` | PUT | 需授权; `file`(multipart/form-data) 或 `avatarBase64`(string) | `avatar`(string, url) | `ProfileActivity.kt` |
| 个人统计（折线/饼图） | `/api/me/stats` | GET | 需授权; `range`(enum: all, month, custom, 选填), `from`(YYYY-MM-DD, 选填), `to`(YYYY-MM-DD, 选填) | `line.items[]: { x`(string, 月份或日期)`, y`(number, 总排放kg) }，`pie.items[]: { name`(string: Food/Travel/Utility)`, value`(number, kg) } | `ProfileStatsActivity.kt` |
| 记录-查询 | `/api/records` | GET | 需授权; `type`(enum: Food/Transport/Utilities, 选填), `month`(YYYY-MM, 选填), `page`(number, 选填), `pageSize`(number, 选填) | `items[]: { id`(string)`, date`(string, YYYY-MM-DD)`, type`(enum)`, amount`(number, "kg CO₂e")`, description`(string) }，`total`(number) | `EmissionRecordsActivity.kt` |
| 记录-创建(食物) | `/api/records/food` | POST | 需授权; `foodName`(string, 必填), `amount`(number, kg, 必填), `note?`(string), `image?`(multipart) | `Record`: `id`, `date`, `type`("Food"), `amount`(kg CO₂e), `unit`("kg CO₂e"), `description` | `AddFoodActivity.kt` |
| 记录-创建(出行) | `/api/records/travel` | POST | 需授权; `mode`(enum: car/bus/cycle/mrt/ship/airplane, 必填), `origin`(string, 必填), `destination`(string, 必填), `distance?`(number, km；如后端可算可选), `originLat?`(number), `originLng?`(number), `destinationLat?`(number), `destinationLng?`(number), `note?`(string) | `Record`: `id`, `date`, `type`("Transport"), `amount`(kg CO₂e), `unit`, `description` | `AddTravelActivity.kt` |
| 记录-创建(水电) | `/api/records/utilities` | POST | 需授权; `electricityUsage`(number, kWh, 选填), `waterUsage`(number, 立方米, 选填), `month`(YYYY-MM, 必填), `note?`(string) | `Record`: `id`, `date`(可用月初), `type`("Utilities"), `amount`(kg CO₂e), `unit`, `description` | `AddUtilityActivity.kt` |
| 账单识别（OCR/推断） | `/api/vision/utility-bill` | POST | 需授权; `file`(multipart/form-data, 必填) | `electricityUsage?`(number, kWh), `waterUsage?`(number, 立方米), `month?`(YYYY-MM), `confidence?`(number, 0-1) | `AddUtilityActivity.kt` |
| 排行榜-列表 | `/api/leaderboard` | GET | `period`(enum: daily/monthly, 选填，默认 monthly), `limit`(number, 选填) | `items[]: { rank`(number)`, username`(string)`, nickname?`(string)`, `avatarUrl?`(string), `pointsToday?`(number), `pointsMonth?`(number), `pointsTotal`(number) } | `LeaderboardActivity.kt` |
| AI 助手对话 | `/api/assistant/chat` | POST | 需授权; `messages[]: { role`("user"|"assistant")`, content`(string) }，可选 `stream`(boolean) | `message: { role`("assistant")`, content`(string) }` 或流式分片 | `AiAssistantActivity.kt` |
| 步数转树木（兑换） | `/api/trees/convert-steps` | POST | 需授权; `steps`(number, 必填) | `currentTreeGrowth`(number, 0-100), `totalPlantedTrees`(number), `todaySteps`(number) | `TreePlantingActivity.kt` |
| 种树进度/统计 | `/api/trees/stats` | GET | 需授权 | `currentTreeGrowth`(number, %), `totalPlantedTrees`(number), `todaySteps`(number) | `TreePlantingActivity.kt` |

附加说明：
- 移动端当前未落地网络层，建议后续以 Retrofit+OkHttp 实现；`Authorization: Bearer <token>` 统一携带在拦截器中。
- `/api/records/travel` 可支持两种距离来源：前端通过 Google Maps/Places 计算后传入，或后端通过经纬度/地址解析计算（推荐保留 `distance` 可选并在缺省时由后端计算）。
- 与 Web 端对齐响应格式：建议统一返回 `{ code, message, data }` 包裹；或直接返回业务对象，但需保持字段稳定（参考根目录 `WEB_API_REQUIREMENTS.md`）。

