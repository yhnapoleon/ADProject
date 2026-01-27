# 专门测试地图功能和账单功能的测试脚本

$baseUrl = "http://localhost:5133"
$testResults = @{
    Travel = @{ Total = 0; Passed = 0; Failed = 0; Errors = @() }
    Bills = @{ Total = 0; Passed = 0; Failed = 0; Errors = @() }
}

function Test-TravelAPI {
    param(
        [string]$Name,
        [scriptblock]$TestScript
    )
    
    $testResults.Travel.Total++
    Write-Host "`n[Travel-$($testResults.Travel.Total)] 测试: $Name" -ForegroundColor Cyan
    
    try {
        & $TestScript
        $testResults.Travel.Passed++
        Write-Host "✓ $Name - 通过" -ForegroundColor Green
        return $true
    } catch {
        $testResults.Travel.Failed++
        $errorMsg = "$Name - 失败: $($_.Exception.Message)"
        $testResults.Travel.Errors += $errorMsg
        Write-Host "✗ $errorMsg" -ForegroundColor Red
        return $false
    }
}

function Test-BillAPI {
    param(
        [string]$Name,
        [scriptblock]$TestScript
    )
    
    $testResults.Bills.Total++
    Write-Host "`n[Bill-$($testResults.Bills.Total)] 测试: $Name" -ForegroundColor Cyan
    
    try {
        & $TestScript
        $testResults.Bills.Passed++
        Write-Host "✓ $Name - 通过" -ForegroundColor Green
        return $true
    } catch {
        $testResults.Bills.Failed++
        $errorMsg = "$Name - 失败: $($_.Exception.Message)"
        $testResults.Bills.Errors += $errorMsg
        $testResults.Bills.Errors += $errorMsg
        Write-Host "✗ $errorMsg" -ForegroundColor Red
        return $false
    }
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "  地图功能和账单功能专项测试" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# ============================================
# 用户认证
# ============================================
Write-Host "`n=== 用户认证 ===" -ForegroundColor Magenta

$timestamp = Get-Date -Format 'yyyyMMddHHmmss'
$username = "testuser$timestamp"
$email = "test$timestamp@example.com"
$password = "Test123!"

try {
    $registerBody = @{
        username = $username
        email = $email
        password = $password
    } | ConvertTo-Json
    
    $null = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✓ 用户注册成功: $username" -ForegroundColor Green
} catch {
    Write-Host "⚠ 注册失败，尝试登录..." -ForegroundColor Yellow
}

$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "✓ 登录成功，Token已获取" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# ============================================
# 地图功能测试 (Travel)
# ============================================
Write-Host "`n=== 地图功能测试 (Travel) ===" -ForegroundColor Magenta

$travelId = $null

Test-TravelAPI "预览路线（不保存）" {
    $previewBody = @{
        originAddress = "Singapore Changi Airport"
        destinationAddress = "Marina Bay Sands, Singapore"
        transportMode = 3  # Car
    } | ConvertTo-Json
    
    $previewResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Body $previewBody -Headers $headers -ContentType "application/json"
    
    if (-not $previewResponse.distanceKilometers) {
        throw "预览失败：未返回距离信息"
    }
    Write-Host "  距离: $($previewResponse.distanceKilometers) km" -ForegroundColor Gray
    Write-Host "  预估碳排放: $($previewResponse.estimatedCarbonEmission) kg CO2" -ForegroundColor Gray
    Write-Host "  预计时间: $($previewResponse.durationText)" -ForegroundColor Gray
    if ($previewResponse.routePolyline) {
        Write-Host "  路线Polyline: $($previewResponse.routePolyline.Substring(0, [Math]::Min(50, $previewResponse.routePolyline.Length)))..." -ForegroundColor Gray
    }
}

Test-TravelAPI "创建出行记录" {
    $createBody = @{
        originAddress = "Orchard Road, Singapore"
        destinationAddress = "Sentosa Island, Singapore"
        transportMode = 0  # Walking
        notes = "测试出行记录"
    } | ConvertTo-Json
    
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method POST -Body $createBody -Headers $headers -ContentType "application/json"
    
    if (-not $createResponse.id) {
        throw "创建失败：未返回ID"
    }
    $script:travelId = $createResponse.id
    Write-Host "  出行记录ID: $travelId" -ForegroundColor Gray
    Write-Host "  距离: $($createResponse.distanceKilometers) km" -ForegroundColor Gray
    Write-Host "  碳排放: $($createResponse.carbonEmission) kg CO2" -ForegroundColor Gray
}

Test-TravelAPI "获取出行记录列表" {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels" -Method GET -Headers $headers
    
    if (-not $listResponse.items) {
        throw "获取列表失败：未返回items"
    }
    Write-Host "  记录数: $($listResponse.items.Count)" -ForegroundColor Gray
    Write-Host "  总数: $($listResponse.totalCount)" -ForegroundColor Gray
}

Test-TravelAPI "获取单条出行记录详情" {
    if (-not $travelId) {
        throw "没有可用的出行记录ID"
    }
    
    $detailResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/$travelId" -Method GET -Headers $headers
    
    if (-not $detailResponse.id) {
        throw "获取详情失败：未返回ID"
    }
    Write-Host "  记录ID: $($detailResponse.id)" -ForegroundColor Gray
    Write-Host "  出发地: $($detailResponse.originAddress)" -ForegroundColor Gray
    Write-Host "  目的地: $($detailResponse.destinationAddress)" -ForegroundColor Gray
    Write-Host "  出行方式: $($detailResponse.transportModeName)" -ForegroundColor Gray
}

Test-TravelAPI "获取出行统计信息" {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method GET -Headers $headers
    
    if ($null -eq $statsResponse.totalRecords) {
        throw "获取统计失败：未返回totalRecords"
    }
    Write-Host "  总记录数: $($statsResponse.totalRecords)" -ForegroundColor Gray
    Write-Host "  总距离: $($statsResponse.totalDistanceKilometers) km" -ForegroundColor Gray
    Write-Host "  总碳排放: $($statsResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
}

# ============================================
# 水电账单功能测试 (UtilityBill)
# ============================================
Write-Host "`n=== 水电账单功能测试 (UtilityBill) ===" -ForegroundColor Magenta

$billId = $null

Test-BillAPI "手动创建水电账单" {
    $billBody = @{
        billType = 0  # Electricity
        billPeriodStart = "2025-01-01T00:00:00Z"
        billPeriodEnd = "2025-01-31T23:59:59Z"
        electricityUsage = 150.5
        waterUsage = 10.2
        gasUsage = 25.3
    } | ConvertTo-Json
    
    $billResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/manual" -Method POST -Body $billBody -Headers $headers -ContentType "application/json"
    
    if (-not $billResponse.id) {
        throw "创建失败：未返回ID"
    }
    $script:billId = $billResponse.id
    Write-Host "  账单ID: $billId" -ForegroundColor Gray
    Write-Host "  账单类型: $($billResponse.billTypeName)" -ForegroundColor Gray
    Write-Host "  账单周期: $($billResponse.billPeriodStart) 至 $($billResponse.billPeriodEnd)" -ForegroundColor Gray
    Write-Host "  总碳排放: $($billResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
}

Test-BillAPI "获取水电账单列表" {
    $billListResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/my-bills" -Method GET -Headers $headers
    
    if (-not $billListResponse.items) {
        throw "获取列表失败：未返回items"
    }
    Write-Host "  记录数: $($billListResponse.items.Count)" -ForegroundColor Gray
    Write-Host "  总数: $($billListResponse.totalCount)" -ForegroundColor Gray
}

Test-BillAPI "获取单条账单详情" {
    if (-not $billId) {
        throw "没有可用的账单ID"
    }
    
    $detailResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/$billId" -Method GET -Headers $headers
    
    if (-not $detailResponse.id) {
        throw "获取详情失败：未返回ID"
    }
    Write-Host "  账单ID: $($detailResponse.id)" -ForegroundColor Gray
    Write-Host "  账单类型: $($detailResponse.billTypeName)" -ForegroundColor Gray
    Write-Host "  用电量: $($detailResponse.electricityUsage) kWh" -ForegroundColor Gray
    Write-Host "  用水量: $($detailResponse.waterUsage) m³" -ForegroundColor Gray
    Write-Host "  总碳排放: $($detailResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
}

Test-BillAPI "获取水电账单统计信息" {
    $billStatsResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/statistics" -Method GET -Headers $headers
    
    if ($null -eq $billStatsResponse.totalRecords) {
        throw "获取统计失败：未返回totalRecords"
    }
    Write-Host "  总记录数: $($billStatsResponse.totalRecords)" -ForegroundColor Gray
    Write-Host "  总用电量: $($billStatsResponse.totalElectricityUsage) kWh" -ForegroundColor Gray
    Write-Host "  总用水量: $($billStatsResponse.totalWaterUsage) m³" -ForegroundColor Gray
    Write-Host "  总碳排放: $($billStatsResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
}

Test-BillAPI "上传水电账单文件（OCR识别）" {
    $testImagePath = "E:\OneDrive\Desktop\AD\Test.jpg"
    
    if (-not (Test-Path $testImagePath)) {
        Write-Host "  跳过：测试图片不存在 ($testImagePath)" -ForegroundColor Yellow
        Write-Host "  提示：可以手动测试，使用命令: curl -X POST `"$baseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@图片路径`"" -ForegroundColor Gray
        return
    }
    
    # 使用curl上传文件
    $tempResponseFile = [System.IO.Path]::GetTempFileName() + ".json"
    $curlPath = "C:\Windows\System32\curl.exe"
    
    $curlCommand = "& `"$curlPath`" -X POST `"$baseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@$testImagePath`" -o `"$tempResponseFile`" -s"
    Invoke-Expression $curlCommand
    
    if (Test-Path $tempResponseFile) {
        $responseContent = Get-Content $tempResponseFile -Raw
        $uploadResponse = $responseContent | ConvertFrom-Json
        
        if (-not $uploadResponse.id) {
            throw "上传失败：未返回ID"
        }
        Write-Host "  账单ID: $($uploadResponse.id)" -ForegroundColor Gray
        Write-Host "  账单类型: $($uploadResponse.billTypeName)" -ForegroundColor Gray
        Write-Host "  账单周期: $($uploadResponse.billPeriodStart) 至 $($uploadResponse.billPeriodEnd)" -ForegroundColor Gray
        Write-Host "  输入方式: $($uploadResponse.inputMethodName)" -ForegroundColor Gray
        if ($uploadResponse.ocrConfidence) {
            Write-Host "  OCR置信度: $([math]::Round($uploadResponse.ocrConfidence * 100, 2))%" -ForegroundColor Gray
        }
        Write-Host "  总碳排放: $($uploadResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
        
        # 保存上传的账单ID用于后续删除
        if (-not $script:billId) {
            $script:billId = $uploadResponse.id
        }
        
        Remove-Item $tempResponseFile -Force -ErrorAction SilentlyContinue
    } else {
        throw "上传失败：未收到响应"
    }
}

# ============================================
# 清理测试数据
# ============================================
Write-Host "`n=== 清理测试数据 ===" -ForegroundColor Magenta

if ($travelId) {
    try {
        $null = Invoke-RestMethod -Uri "$baseUrl/api/travel/$travelId" -Method DELETE -Headers $headers
        Write-Host "✓ 已删除出行记录: $travelId" -ForegroundColor Green
    } catch {
        Write-Host "⚠ 删除出行记录失败: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

if ($billId) {
    try {
        $null = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/$billId" -Method DELETE -Headers $headers
        Write-Host "✓ 已删除水电账单: $billId" -ForegroundColor Green
    } catch {
        Write-Host "⚠ 删除水电账单失败: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# ============================================
# 测试结果总结
# ============================================
Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "  测试结果总结" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

Write-Host "`n【地图功能 (Travel)】" -ForegroundColor Cyan
Write-Host "总测试数: $($testResults.Travel.Total)" -ForegroundColor White
Write-Host "通过: $($testResults.Travel.Passed)" -ForegroundColor Green
Write-Host "失败: $($testResults.Travel.Failed)" -ForegroundColor $(if ($testResults.Travel.Failed -eq 0) { "Green" } else { "Red" })
if ($testResults.Travel.Total -gt 0) {
    Write-Host "成功率: $([Math]::Round($testResults.Travel.Passed / $testResults.Travel.Total * 100, 2))%" -ForegroundColor $(if ($testResults.Travel.Failed -eq 0) { "Green" } else { "Yellow" })
}
if ($testResults.Travel.Errors.Count -gt 0) {
    Write-Host "失败详情:" -ForegroundColor Red
    foreach ($error in $testResults.Travel.Errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
}

Write-Host "`n【水电账单功能 (UtilityBill)】" -ForegroundColor Cyan
Write-Host "总测试数: $($testResults.Bills.Total)" -ForegroundColor White
Write-Host "通过: $($testResults.Bills.Passed)" -ForegroundColor Green
Write-Host "失败: $($testResults.Bills.Failed)" -ForegroundColor $(if ($testResults.Bills.Failed -eq 0) { "Green" } else { "Red" })
if ($testResults.Bills.Total -gt 0) {
    Write-Host "成功率: $([Math]::Round($testResults.Bills.Passed / $testResults.Bills.Total * 100, 2))%" -ForegroundColor $(if ($testResults.Bills.Failed -eq 0) { "Green" } else { "Yellow" })
}
if ($testResults.Bills.Errors.Count -gt 0) {
    Write-Host "失败详情:" -ForegroundColor Red
    foreach ($error in $testResults.Bills.Errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
}

Write-Host "`nTest completed!" -ForegroundColor Green
