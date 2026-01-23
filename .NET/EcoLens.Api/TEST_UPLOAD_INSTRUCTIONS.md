# 账单图片上传测试说明

## 重要提示

我已经修复了文件上传的大小限制问题，但**需要重启项目**才能生效。

## 测试步骤

### 1. 停止当前运行的项目
- 在运行项目的终端窗口按 `Ctrl+C` 停止项目
- 或者关闭运行项目的窗口

### 2. 重新启动项目
```powershell
cd "E:\OneDrive\Desktop\AD\ADProject\.NET\EcoLens.Api"
dotnet run
```

等待项目完全启动（看到 "Now listening on: http://localhost:5133"）

### 3. 运行上传测试
在新的PowerShell窗口中运行：
```powershell
cd "E:\OneDrive\Desktop\AD\ADProject\.NET\EcoLens.Api"
powershell -ExecutionPolicy Bypass -File upload-test-final.ps1 -FilePath "E:\OneDrive\Desktop\AD\Test.jpg"
```

## 预期结果

根据你的账单图片，系统应该提取：
- **电费：** 517 kWh → 碳排放：209.75 kg CO2
- **水费：** 8.2 m³ → 碳排放：3.44 kg CO2  
- **燃气：** 0 kWh → 碳排放：0 kg CO2
- **总计：** 213.18 kg CO2
- **账单周期：** 2025-11-05 到 2025-12-05

## 如果测试失败

如果OCR识别不准确，可以：
1. 查看OCR识别的原始文本（在日志中）
2. 使用手动输入功能补充数据
3. 告诉我识别结果，我可以优化解析逻辑

## 替代测试方法

如果PowerShell脚本有问题，可以使用：

### 方法1：使用Postman
1. POST `http://localhost:5133/api/auth/login`
   - Body: `{"email":"test@example.com","password":"Test123!"}`
   - 复制返回的token

2. POST `http://localhost:5133/api/UtilityBill/upload`
   - Headers: `Authorization: Bearer YOUR_TOKEN`
   - Body: form-data
   - Key: `file` (类型: File)
   - Value: 选择 `E:\OneDrive\Desktop\AD\Test.jpg`

### 方法2：使用curl
```powershell
# 1. 登录获取token
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5133/api/auth/login" -Method POST -Body '{"email":"test@example.com","password":"Test123!"}' -ContentType "application/json"
$token = $loginResponse.token

# 2. 上传文件
curl -X POST "http://localhost:5133/api/UtilityBill/upload" -H "Authorization: Bearer $token" -F "file=@E:\OneDrive\Desktop\AD\Test.jpg"
```
