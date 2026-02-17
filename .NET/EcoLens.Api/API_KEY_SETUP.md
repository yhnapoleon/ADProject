# Google Maps API Key 配置说明

## 📋 概述

本项目使用 Google Maps API 来实现地图功能。为了安全，API Key 不会提交到 Git 仓库。每个开发者需要自己配置 API Key。

---

## 🚀 快速开始（3步完成）

### 第1步：复制模板文件

在项目根目录 `.NET/EcoLens.Api/` 下，复制 `appsettings.example.json` 文件：

**Windows:**
```powershell
copy appsettings.example.json appsettings.Development.json
```

**Mac/Linux:**
```bash
cp appsettings.example.json appsettings.Development.json
```

或者直接在文件管理器中复制粘贴并重命名。

---

### 第2步：获取 API Key

联系项目负责人获取 Google Maps API Key。

**API Key 格式示例：**
```
AIzaSyAb7zNgT5dlWV1ouP0fdoOSk3QpN0Q-sHQ
```

---

### 第3步：填写 API Key

打开 `appsettings.Development.json` 文件，找到这一行：

```json
"ApiKey": "YOUR_API_KEY_HERE"
```

将 `YOUR_API_KEY_HERE` 替换为你的真实 API Key：

```json
"ApiKey": "AIzaSyAb7zNgT5dlWV1ouP0fdoOSk3QpN0Q-sHQ"
```

保存文件。

---

## ✅ 验证配置

### 方法1：运行项目测试

1. 启动项目：
   ```bash
   dotnet run
   ```

2. 如果项目正常启动，说明配置成功。

### 方法2：测试 API Key（可选）

在浏览器中访问（替换 `YOUR_API_KEY` 为你的真实 Key）：

```
https://maps.googleapis.com/maps/api/geocode/json?address=北京&key=YOUR_API_KEY
```

如果返回 JSON 数据，说明 API Key 有效。

---

## 📁 文件说明

| 文件 | 说明 | 是否提交到 Git |
|------|------|----------------|
| `appsettings.json` | 基础配置文件 | ✅ 是（不包含真实 Key） |
| `appsettings.example.json` | 配置模板文件 | ✅ 是 |
| `appsettings.Development.json` | 开发环境配置（包含真实 Key） | ❌ 否（已忽略） |

---

## ⚠️ 重要提示

1. **不要提交 API Key 到 Git**
   - `appsettings.Development.json` 已在 `.gitignore` 中
   - 不要手动添加到 Git

2. **不要分享 API Key**
   - API Key 是个人使用的
   - 不要公开分享或发布到网上

3. **如果 API Key 泄露**
   - 立即联系项目负责人
   - 在 Google Cloud Console 中删除并重新创建

---

## 🆘 常见问题

### Q1: 找不到 `appsettings.example.json` 文件？

**A:** 确保你在 `.NET/EcoLens.Api/` 目录下查找。如果确实没有，可以手动创建，参考上面的模板。

### Q2: 项目启动后报错 "GoogleMaps:ApiKey 未配置"？

**A:** 检查：
- `appsettings.Development.json` 文件是否存在
- API Key 是否正确填写（没有多余空格）
- 文件格式是否正确（JSON 格式）

### Q3: 可以多人共用一个 API Key 吗？

**A:** 可以，但建议：
- 开发环境可以共用（节省配额）
- 生产环境必须每人一个（安全考虑）

### Q4: API Key 有使用限制吗？

**A:** Google Maps API 有免费额度：
- 每月 $200 免费额度
- 超出后按使用量收费
- 建议设置预算提醒

---

## 📞 需要帮助？

如果遇到问题，请联系：
- 项目负责人
- 查看项目文档
- 查看 Google Maps API 官方文档

---

## 📝 更新日志

- 2024-XX-XX: 初始版本

---

**配置完成后，就可以正常使用地图功能了！** 🎉
