================================================================================
EcoLens - 本地运行说明 (How to Run the Product Locally)
================================================================================

本文件夹包含产品本地运行所需的数据库脚本（架构 + 种子数据），便于讲师或评审在无法连接 Azure 时，在本地 SQL Server 上还原数据库并运行完整系统。

This folder contains the database scripts (schema + seed data) required to run the product locally, so that instructors or reviewers can restore the database and run the full system without connecting to Azure.

--------------------------------------------------------------------------------
【演示账号 / Demo Account】— 执行 SeedData.sql 后可用
--------------------------------------------------------------------------------
  登录入口：前端页面选择 "Log In"，或访问 /login

  邮箱 (Email):    demo@ecolens.local
  密码 (Password): Demo123!

  说明：
  - 该账号仅用于本地演示与评审，密码为上述固定值，便于测试。
  - 请勿在生产或 Azure 环境中使用；生产环境不包含此账号。
  - 如需更多测试账号，可在前端 "Register" 页面自行注册。

--------------------------------------------------------------------------------
1. 环境要求 (Prerequisites)
--------------------------------------------------------------------------------
  - .NET 8 SDK
  - Node.js 18+（前端）
  - SQL Server 2019+ 或 SQL Server LocalDB / SQL Express（本地数据库）
  - 编辑器或命令行（用于执行 SQL、修改配置）
  - 可选：生产/演示环境才需要连接 Azure

--------------------------------------------------------------------------------
2. 数据库准备 (Database Setup)
--------------------------------------------------------------------------------
2.1 创建空数据库
    在 SQL Server Management Studio (SSMS)、Azure Data Studio 或 sqlcmd 中执行：
    CREATE DATABASE EcoLensDb;
    GO

2.2 执行脚本顺序（重要）
    在本 Database 文件夹中，按以下顺序执行 SQL 文件：
    ① 先执行 Schema.sql
       - 创建所有表、索引、外键、EF 迁移历史
       - 包含内置种子数据（如 CarbonReferences、SystemSettings 等）
    ② 再执行 SeedData.sql
       - 插入演示用测试用户（即上面的演示账号 demo@ecolens.local）

    执行方式示例（SSMS）：打开 .sql 文件 → 连接到本地 SQL Server → 选择数据库 EcoLensDb → 执行(F5)。

2.3 配置后端连接字符串
    在项目 .NET/EcoLens.Api 中配置连接字符串，使后端指向本地数据库：
    - 若有 appsettings.Development.json，在其中设置 ConnectionStrings:DefaultConnection；
    - 若无，可复制 appsettings.json 为 appsettings.Development.json 再修改。
    推荐内容（按实际实例名调整）：
      "ConnectionStrings": {
        "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EcoLensDb;Trusted_Connection=True;TrustServerCertificate=True;"
      }
    或使用本机默认实例：
      "DefaultConnection": "Server=.;Database=EcoLensDb;Integrated Security=true;TrustServerCertificate=True;"

--------------------------------------------------------------------------------
3. 后端运行 (Backend)
--------------------------------------------------------------------------------
  在项目根目录打开终端：
  cd .NET/EcoLens.Api
  dotnet restore
  dotnet run --launch-profile http

  成功启动后：
  - 后端地址: http://localhost:5133
  - API 文档: http://localhost:5133/swagger

--------------------------------------------------------------------------------
4. 前端运行 (Frontend)
--------------------------------------------------------------------------------
  另开一个终端，在项目根目录：
  cd web
  npm install
  npm run dev

  浏览器打开终端中显示的本地地址（如 http://localhost:5173）。

  若要让前端请求本地后端，在 web 目录下创建 .env.local，内容为：
  VITE_API_URL=http://localhost:5133/api

--------------------------------------------------------------------------------
5. 登录与验证
--------------------------------------------------------------------------------
  - 打开前端登录页，使用上述【演示账号】登录：
    邮箱：demo@ecolens.local
    密码：Demo123!
  - 登录后可访问仪表盘、记录、排行榜、AI 助手等功能，用于演示与评审。

--------------------------------------------------------------------------------
6. 本文件夹文件说明 (Files in This Folder)
--------------------------------------------------------------------------------
  - Schema.sql   : 架构脚本。包含所有 CREATE TABLE、索引、外键、EF 迁移历史及迁移中定义的种子数据。
  - SeedData.sql : 种子/演示数据。包含演示用户（demo@ecolens.local），不含真实敏感数据。
  - readme.txt   : 本说明文件（如何在本地运行产品 / how to run the product locally）。

================================================================================
