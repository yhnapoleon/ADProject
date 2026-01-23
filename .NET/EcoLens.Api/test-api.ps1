# API 测试脚本
$baseUrl = "http://localhost:5133"
$token = ""

Write-Host "=== 开始 API 测试 ===" -ForegroundColor Green

# 等待项目启动
Write-Host "等待项目启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 1. 测试注册
Write-Host "`n1. 测试用户注册..." -ForegroundColor Cyan
try {
    $registerBody = '{"username":"testuser","email":"test@example.com","password":"Test123!"}'
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    Write-Host "注册成功！Token: $($registerResponse.token.Substring(0, 20))..." -ForegroundColor Green
    $token = $registerResponse.token
} catch {
    Write-Host "注册失败或用户已存在，尝试登录..." -ForegroundColor Yellow
    try {
        $loginBody = '{"email":"test@example.com","password":"Test123!"}'
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
        $token = $loginResponse.token
        Write-Host "登录成功！Token: $($token.Substring(0, 20))..." -ForegroundColor Green
    } catch {
        Write-Host "登录也失败: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

if ([string]::IsNullOrEmpty($token)) {
    Write-Host "无法获取Token，测试终止" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. 测试预览路线
Write-Host "`n2. 测试预览路线..." -ForegroundColor Cyan
try {
    $previewBody = '{"originAddress":"北京市朝阳区","destinationAddress":"北京市海淀区","transportMode":3}'
    $previewResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method Post -Body $previewBody -Headers $headers -ContentType "application/json"
    Write-Host "预览成功！距离: $($previewResponse.distanceKilometers) km, 碳排放: $($previewResponse.estimatedCarbonEmission) kg CO2" -ForegroundColor Green
} catch {
    Write-Host "预览失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. 测试创建出行记录（步行）
Write-Host "`n3. 测试创建出行记录（步行）..." -ForegroundColor Cyan
try {
    $createBody = '{"originAddress":"北京市天安门广场","destinationAddress":"北京市故宫博物院","transportMode":0,"notes":"步行游览"}'
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method Post -Body $createBody -Headers $headers -ContentType "application/json"
    $travelLogId = $createResponse.id
    Write-Host "创建成功！记录ID: $travelLogId, 距离: $($createResponse.distanceKilometers) km, 碳排放: $($createResponse.carbonEmission) kg CO2" -ForegroundColor Green
} catch {
    Write-Host "创建失败: $($_.Exception.Message)" -ForegroundColor Red
    $travelLogId = $null
}

# 4. 测试获取列表
Write-Host "`n4. 测试获取出行记录列表..." -ForegroundColor Cyan
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels" -Method Get -Headers $headers
    Write-Host "获取成功！记录数: $($listResponse.items.Count)" -ForegroundColor Green
    if ($listResponse.items.Count -gt 0) {
        Write-Host "第一条记录: $($listResponse.items[0].originAddress) -> $($listResponse.items[0].destinationAddress)" -ForegroundColor Gray
    }
} catch {
    Write-Host "获取列表失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. 测试获取统计
Write-Host "`n5. 测试获取统计信息..." -ForegroundColor Cyan
try {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method Get -Headers $headers
    Write-Host "统计成功！总记录数: $($statsResponse.totalRecords), 总距离: $($statsResponse.totalDistanceKilometers) km, 总碳排放: $($statsResponse.totalCarbonEmission) kg CO2" -ForegroundColor Green
} catch {
    Write-Host "获取统计失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 6. 测试筛选功能
Write-Host "`n6. 测试筛选功能（按出行方式）..." -ForegroundColor Cyan
try {
    $filterResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?transportMode=0&page=1&pageSize=10" -Method Get -Headers $headers
    Write-Host "筛选成功！记录数: $($filterResponse.items.Count), 总记录数: $($filterResponse.totalCount)" -ForegroundColor Green
} catch {
    Write-Host "筛选失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Green
