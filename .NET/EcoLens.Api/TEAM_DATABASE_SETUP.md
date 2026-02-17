# 团队数据库连接配置指南

## 📋 数据库连接信息

### 基本信息
- **服务器地址**: `ecolens.database.windows.net`
- **数据库名**: `ecolens`
- **端口**: `1433`
- **认证方式**: SQL 认证

### 登录凭据
- **用户名**: `ecolensadmin`
- **密码**: `EcoLens2025!`

### 完整连接字符串
```
Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;Persist Security Info=False;User ID=ecolensadmin;Password=EcoLens2025!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

---

## 🚀 配置步骤

### 步骤 1: 获取你的公网 IP 地址

**方法一：使用网站**
1. 打开浏览器访问：https://whatismyipaddress.com/
2. 记录显示的 IPv4 地址

**方法二：使用命令行（PowerShell）**
```powershell
(Invoke-WebRequest -Uri "https://api.ipify.org").Content
```

**方法三：使用命令行（CMD）**
```cmd
curl ifconfig.me
```

### 步骤 2: 将 IP 地址添加到防火墙规则

**重要：** 需要项目负责人或拥有 Azure 访问权限的成员添加你的 IP 地址。

1. 登录 Azure 门户：https://portal.azure.com
2. 搜索并进入 SQL Server：`ecolens`
3. 左侧菜单：**安全性** → **网络**
4. 选择 **公共访问** 选项卡
5. 在 **防火墙规则** 部分，点击 **+ 添加客户端 IPv4 地址** 或 **+ 添加防火墙规则**
6. 填写信息：
   - **规则名称**: 例如 `YourName-Laptop` 或 `YourName-Desktop`
   - **起始 IP 地址**: 你的公网 IP（例如：`137.132.26.72`）
   - **结束 IP 地址**: 与起始 IP 相同（单个 IP）
7. 点击 **添加**，然后点击页面顶部的 **保存**

**注意：** 如果你的 IP 地址是动态的（每次连接可能变化），需要每次变化后重新添加。

### 步骤 3: 克隆/更新项目代码

```bash
# 如果是第一次克隆
git clone <仓库地址>
cd ADProject

# 如果已有项目，更新到最新版本
git pull origin main
```

### 步骤 4: 配置本地连接字符串

1. 打开文件：`.NET/EcoLens.Api/appsettings.json`
2. 找到 `ConnectionStrings` → `DefaultConnection`
3. 替换为以下内容：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;Persist Security Info=False;User ID=ecolensadmin;Password=EcoLens2025!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

**重要：** 
- 不要将包含密码的 `appsettings.json` 提交到 Git
- 确保 `.gitignore` 已包含 `appsettings.json`（通常已配置）

### 步骤 5: 运行数据库迁移

```bash
cd .NET/EcoLens.Api
dotnet restore
dotnet ef database update
```

如果遇到错误，请参考 **常见问题** 部分。

### 步骤 6: 测试连接

1. **启动应用**：
   ```bash
   dotnet run
   ```

2. **访问 Swagger 文档**：
   - 打开浏览器访问：http://localhost:5133/swagger

3. **测试 API**：
   - 测试用户注册：`POST /api/auth/register`
   - 测试用户登录：`POST /api/auth/login`
   - 测试获取用户信息：`GET /api/users/profile`

---

## ✅ 配置检查清单

在开始开发前，请确认：

- [ ] 你的 IP 地址已添加到 Azure 防火墙规则
- [ ] 已更新本地 `appsettings.json` 中的连接字符串
- [ ] 已运行 `dotnet ef database update` 且无错误
- [ ] 应用可以正常启动
- [ ] Swagger 文档可以正常访问
- [ ] 可以成功调用 API（如用户注册）

---

## 🔧 常见问题

### 问题 1: 连接超时或无法连接

**可能原因：**
- IP 地址未添加到防火墙规则
- IP 地址已变化（动态 IP）

**解决方法：**
1. 检查当前 IP 地址是否与添加的 IP 一致
2. 如果 IP 已变化，请重新添加到防火墙规则
3. 确认连接字符串正确

### 问题 2: 数据库迁移失败

**可能原因：**
- 数据库连接失败
- 迁移文件冲突

**解决方法：**
1. 确认连接字符串正确
2. 确认 IP 已添加到防火墙
3. 如果遇到迁移冲突，联系项目负责人

### 问题 3: 认证失败

**可能原因：**
- 用户名或密码错误
- 连接字符串格式错误

**解决方法：**
1. 检查连接字符串中的用户名和密码
2. 确认使用 SQL 认证（不是 Microsoft Entra ID）

### 问题 4: IP 地址经常变化

**解决方法：**
1. 每次 IP 变化后，重新添加到防火墙规则
2. 或者考虑使用 VPN 或固定 IP
3. 如果应用部署在 Azure 上，可以开启"允许 Azure 服务和资源访问此服务器"

---

## 📞 需要帮助？

如果遇到问题，请：

1. **检查本文档的常见问题部分**
2. **联系项目负责人**，提供：
   - 错误信息截图
   - 你的公网 IP 地址
   - 操作步骤
3. **在团队群组中提问**，其他成员可能遇到过类似问题

---

## 🔒 安全提示

1. **不要将密码提交到 Git**
   - 确保 `appsettings.json` 在 `.gitignore` 中
   - 使用 `appsettings.example.json` 作为模板

2. **定期更新密码**
   - 如果密码泄露，立即通知项目负责人更改

3. **不要分享连接字符串**
   - 仅在团队内部安全渠道分享
   - 不要公开发布到论坛或社交媒体

4. **定期审查防火墙规则**
   - 删除不再使用的 IP 地址
   - 确保只有团队成员可以访问

---

## 📝 更新日志

- **2026-01-27**: 初始版本，Azure SQL 数据库配置完成

---

**最后更新**: 2026-01-27  
**维护者**: 项目负责人
