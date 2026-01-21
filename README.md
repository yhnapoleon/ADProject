## EcoLens.Api 项目说明

这是一个使用 .NET 8 的 Web API 项目（目录：`.NET/EcoLens.Api`），使用 SQL Server 作为数据库，启用 JWT 身份认证，并在开发环境下提供 Swagger UI。

### 运行环境要求
- **.NET SDK**: 8.0+
- **数据库**: SQL Server（本机或远程；支持 LocalDB/开发版/容器）
- 可选：VS Code（已提供 `launch.json`），或 Visual Studio 2022

### 关键配置文件
- `/.NET/EcoLens.Api/appsettings.json`
  - ConnectionStrings: `DefaultConnection`
  - Jwt: `Issuer`、`Audience`、`Key`（至少 32 位随机密钥）、`ExpirationMinutes`

示例（已在仓库中提供，可按需修改）:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EcoLensDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "EcoLens",
    "Audience": "EcoLens",
    "Key": "REPLACE_WITH_A_SECURE_RANDOM_KEY_AT_LEAST_32_CHARS",
    "ExpirationMinutes": 60
  }
}
```

> 建议将敏感信息（如数据库连接串、`Jwt:Key`）转移到用户机密或环境变量，避免提交到仓库。

### 使用用户机密（推荐，开发环境）
在项目目录下初始化并设置机密（不会写入仓库）：

```bash
dotnet user-secrets init --project ".NET/EcoLens.Api"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EcoLensDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ".NET/EcoLens.Api"
dotnet user-secrets set "Jwt:Key" "<你的32位以上随机密钥>" --project ".NET/EcoLens.Api"
```

### 数据库迁移与初始化
项目使用 EF Core（`ApplicationDbContext`，SQL Server）。如果仓库未包含迁移，需要先创建迁移再更新数据库：

```bash
# 安装 EF CLI（如未安装）
dotnet tool install --global dotnet-ef

# 在解决方案根目录执行（-p 指定项目，-s 指定启动项目；本项目二者一致）
dotnet ef migrations add InitialCreate -p ".NET/EcoLens.Api" -s ".NET/EcoLens.Api"
dotnet ef database update -p ".NET/EcoLens.Api" -s ".NET/EcoLens.Api"
```

如果迁移已存在，直接执行数据库更新：

```bash
dotnet ef database update -p ".NET/EcoLens.Api" -s ".NET/EcoLens.Api"
```

### 启动项目
开发模式运行（会启用 Swagger UI、开放 CORS）:

```bash
dotnet run --project ".NET/EcoLens.Api"
```

VS Code 中可直接按 F5（已提供 `.vscode/launch.json`）。默认会在终端显示 `Now listening on: http(s)://...`，Swagger UI 地址为：

- `http://localhost:<端口>/swagger`

### 身份认证（JWT）
- 登录/注册接口位于 `AuthController`（具体路由以控制器实现为准）。
- 获取到 JWT 后，在 Swagger 右上角点击 Authorize，输入：`Bearer <你的token>`。
- JWT 校验使用 `Jwt:Issuer`、`Jwt:Audience`、`Jwt:Key`，请确保三者与签发一致。

### CORS
开发环境默认放开所有来源、请求头与方法（策略名：`AllowAll`）。部署到生产时请收窄为受信任域名。

### 常见问题
- 无法推送到 GitHub：请确保已使用仓库根目录的 `.gitignore` 忽略 `bin/`、`obj/`、`.vs/` 等构建产物；若已被跟踪，可执行：

```bash
git rm -r --cached ".NET/EcoLens.Api/bin" ".NET/EcoLens.Api/obj" ".vs"
git add -A
git commit -m "chore: add .gitignore and untrack build artifacts"
```

- 数据库连接失败：检查 `DefaultConnection` 是否可用（网络、防火墙、账号权限、`TrustServerCertificate` 等）。
- JWT 验证失败：确认 `Jwt:Key` 足够强度且与签发端一致；`Issuer`/`Audience` 与令牌匹配。

### 目录结构（节选）
- `.NET/EcoLens.Api`：Web API 项目根目录
- `.vscode/launch.json`：VS Code 调试配置（`ASPNETCORE_ENVIRONMENT=Development`）
- `appsettings.json`：应用配置（数据库、JWT 等）

### 许可证
根据你项目实际选择（如无明确说明，可后续补充 `LICENSE`）。


