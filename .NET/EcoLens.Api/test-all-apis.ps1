# 全面 API 测试脚本 - 测试所有功能模块
# 包括：地图功能、收据识别、活动记录、水电账单

$baseUrl = "http://localhost:5133"
$testResults = @{
    Total = 0
    Passed = 0
    Failed = 0
    Errors = @()
}

function Test-API {
    param(
        [string]$Name,
        [scriptblock]$TestScript
    )
    
    $testResults.Total++
    Write-Host "`n[$($testResults.Total)] 测试: $Name" -ForegroundColor Cyan
    
    try {
        & $TestScript
        $testResults.Passed++
        Write-Host "✓ $Name - 通过" -ForegroundColor Green
        return $true
    } catch {
        $testResults.Failed++
        $errorMsg = "$Name - 失败: $($_.Exception.Message)"
        $testResults.Errors += $errorMsg
        Write-Host "✗ $errorMsg" -ForegroundColor Red
        return $false
    }
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "  全面 API 测试 - EcoLens API" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# ============================================
# 第一步：用户认证
# ============================================
Write-Host "`n=== 第一步：用户认证 ===" -ForegroundColor Magenta

$timestamp = Get-Date -Format 'yyyyMMddHHmmss'
$username = "testuser$timestamp"
$email = "test$timestamp@example.com"
$password = "Test123!"
$token = ""

Test-API "用户注册" {
    $registerBody = @{
        username = $username
        email = $email
        password = $password
    } | ConvertTo-Json
    
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    if (-not $registerResponse.token) {
        throw "注册失败：未返回Token"
    }
    Write-Host "  用户: $username ($email)" -ForegroundColor Gray
}

Test-API "用户登录" {
    $loginBody = @{
        email = $email
        password = $password
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    if (-not $loginResponse.token) {
        throw "登录失败：未返回Token"
    }
    $script:token = $loginResponse.token
    Write-Host "  Token长度: $($token.Length)" -ForegroundColor Gray
}

if ([string]::IsNullOrEmpty($token)) {
    Write-Host "`n✗ 无法获取Token，测试终止" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# ============================================
# 第二步：地图功能测试 (TravelController)
# ============================================
Write-Host "`n=== 第二步：地图功能测试 ===" -ForegroundColor Magenta

$travelId = $null

Test-API "预览路线（不保存）" {
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
    if ($previewResponse.routePolyline) {
        Write-Host "  路线Polyline: $($previewResponse.routePolyline.Substring(0, [Math]::Min(50, $previewResponse.routePolyline.Length)))..." -ForegroundColor Gray
    }
}

Test-API "创建出行记录" {
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

Test-API "获取出行记录列表" {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels" -Method GET -Headers $headers
    
    if (-not $listResponse.items) {
        throw "获取列表失败：未返回items"
    }
    Write-Host "  记录数: $($listResponse.items.Count)" -ForegroundColor Gray
    Write-Host "  总数: $($listResponse.totalCount)" -ForegroundColor Gray
}

Test-API "获取单条出行记录详情" {
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
}

Test-API "获取出行统计信息" {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method GET -Headers $headers
    
    if ($null -eq $statsResponse.totalRecords) {
        throw "获取统计失败：未返回totalRecords"
    }
    Write-Host "  总记录数: $($statsResponse.totalRecords)" -ForegroundColor Gray
    Write-Host "  总距离: $($statsResponse.totalDistanceKilometers) km" -ForegroundColor Gray
    Write-Host "  总碳排放: $($statsResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
}

# ============================================
# 第三步：收据识别测试 (VisionController)
# ============================================
Write-Host "`n=== 第三步：收据识别测试 ===" -ForegroundColor Magenta

Test-API "分析图片（Vision API）" {
    # 创建一个简单的测试图片文件（1x1像素的PNG）
    $testImagePath = "$env:TEMP\test-receipt-$(Get-Date -Format 'HHmmss').png"
    
    # 创建一个最小的有效PNG文件（1x1像素，红色）
    $pngBytes = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==")
    [System.IO.File]::WriteAllBytes($testImagePath, $pngBytes)
    
    try {
        # 使用curl上传文件（PowerShell 5.1兼容）
        $tempResponseFile = [System.IO.Path]::GetTempFileName() + ".json"
        $curlPath = "C:\Windows\System32\curl.exe"
        
        $curlCommand = "& `"$curlPath`" -X POST `"$baseUrl/api/vision/analyze`" -H `"Authorization: Bearer $token`" -F `"image=@$testImagePath`" -o `"$tempResponseFile`" -s"
        Invoke-Expression $curlCommand
        
        if (Test-Path $tempResponseFile) {
            $responseContent = Get-Content $tempResponseFile -Raw
            $visionResponse = $responseContent | ConvertFrom-Json
            
            if (-not $visionResponse.label) {
                throw "分析失败：未返回label"
            }
            Write-Host "  识别标签: $($visionResponse.label)" -ForegroundColor Gray
            
            Remove-Item $tempResponseFile -Force -ErrorAction SilentlyContinue
        } else {
            throw "上传失败：未收到响应"
        }
    } finally {
        if (Test-Path $testImagePath) {
            Remove-Item $testImagePath -Force -ErrorAction SilentlyContinue
        }
    }
}

# ============================================
# 第四步：活动记录测试 (ActivityController)
# ============================================
Write-Host "`n=== 第四步：活动记录测试 ===" -ForegroundColor Magenta

$activityId = $null

Test-API "上传活动记录" {
    # ActivityController的upload方法使用[FromForm]，需要使用multipart/form-data
    # 使用curl发送form-data（PowerShell 5.1兼容）
    $tempResponseFile = [System.IO.Path]::GetTempFileName() + ".json"
    $curlPath = "C:\Windows\System32\curl.exe"
    
    $curlCommand = "& `"$curlPath`" -X POST `"$baseUrl/api/activity/upload`" -H `"Authorization: Bearer $token`" -F `"label=Apple`" -F `"quantity=2.5`" -o `"$tempResponseFile`" -s"
    Invoke-Expression $curlCommand
    
    if (Test-Path $tempResponseFile) {
        $responseContent = Get-Content $tempResponseFile -Raw
        $activityResponse = $responseContent | ConvertFrom-Json
        
        if (-not $activityResponse.id) {
            throw "上传失败：未返回ID"
        }
        $script:activityId = $activityResponse.id
        Write-Host "  活动记录ID: $activityId" -ForegroundColor Gray
        Write-Host "  标签: $($activityResponse.label)" -ForegroundColor Gray
        Write-Host "  数量: $($activityResponse.quantity)" -ForegroundColor Gray
        Write-Host "  碳排放: $($activityResponse.totalEmission) kg CO2" -ForegroundColor Gray
        
        Remove-Item $tempResponseFile -Force -ErrorAction SilentlyContinue
    } else {
        throw "上传失败：未收到响应"
    }
}

Test-API "获取活动日志列表" {
    $activityLogsResponse = Invoke-RestMethod -Uri "$baseUrl/api/activity/my-logs" -Method GET -Headers $headers
    
    if ($null -eq $activityLogsResponse) {
        throw "获取列表失败：未返回数据"
    }
    Write-Host "  记录数: $($activityLogsResponse.Count)" -ForegroundColor Gray
}

Test-API "获取活动统计信息" {
    $activityStatsResponse = Invoke-RestMethod -Uri "$baseUrl/api/activity/stats" -Method GET -Headers $headers
    
    if ($null -eq $activityStatsResponse.totalEmission) {
        throw "获取统计失败：未返回totalEmission"
    }
    Write-Host "  总碳排放: $($activityStatsResponse.totalEmission) kg CO2" -ForegroundColor Gray
    Write-Host "  总记录数: $($activityStatsResponse.totalItems)" -ForegroundColor Gray
    Write-Host "  折算树木: $($activityStatsResponse.treesSaved)" -ForegroundColor Gray
    Write-Host "  全服排名: $($activityStatsResponse.rank)" -ForegroundColor Gray
}

Test-API "获取图表数据" {
    $chartResponse = Invoke-RestMethod -Uri "$baseUrl/api/activity/chart-data?days=7" -Method GET -Headers $headers
    
    if ($null -eq $chartResponse -or $chartResponse.Count -eq 0) {
        throw "获取图表数据失败：未返回数据"
    }
    Write-Host "  数据点数: $($chartResponse.Count)" -ForegroundColor Gray
}

Test-API "获取热力图数据" {
    $heatmapResponse = Invoke-RestMethod -Uri "$baseUrl/api/activity/heatmap" -Method GET -Headers $headers
    
    if ($null -eq $heatmapResponse) {
        throw "获取热力图失败：未返回数据"
    }
    Write-Host "  Region count: $($heatmapResponse.Count)" -ForegroundColor Gray
}

# ============================================
# 第五步：水电账单测试 (UtilityBillController)
# ============================================
Write-Host "`n=== 第五步：水电账单测试 ===" -ForegroundColor Magenta

$billId = $null

Test-API "手动创建水电账单" {
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
    Write-Host "  总碳排放: $($billResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
}

Test-API "获取水电账单列表" {
    $billListResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/my-bills" -Method GET -Headers $headers
    
    if (-not $billListResponse.items) {
        throw "获取列表失败：未返回items"
    }
    Write-Host "  记录数: $($billListResponse.items.Count)" -ForegroundColor Gray
    Write-Host "  总数: $($billListResponse.totalCount)" -ForegroundColor Gray
}

Test-API "获取水电账单统计信息" {
    $billStatsResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/statistics" -Method GET -Headers $headers
    
    if ($null -eq $billStatsResponse.totalRecords) {
        throw "获取统计失败：未返回totalRecords"
    }
    Write-Host "  总记录数: $($billStatsResponse.totalRecords)" -ForegroundColor Gray
    Write-Host "  总碳排放: $($billStatsResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
}

Test-API "上传水电账单文件（OCR识别）" {
    # 检查是否有测试图片文件
    $testImagePath = "E:\OneDrive\Desktop\AD\Test.jpg"
    
    if (-not (Test-Path $testImagePath)) {
        Write-Host "  跳过：测试图片不存在 ($testImagePath)" -ForegroundColor Yellow
        Write-Host "  提示：可以手动测试，使用命令: curl -X POST `"$baseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@图片路径`"" -ForegroundColor Gray
        return
    }
    
    # 使用curl上传文件（PowerShell 5.1兼容）
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
# 第六步：清理测试数据
# ============================================
Write-Host "`n=== 第六步：清理测试数据 ===" -ForegroundColor Magenta

if ($travelId) {
    Test-API "删除出行记录" {
        $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/$travelId" -Method DELETE -Headers $headers
        Write-Host "  已删除出行记录: $travelId" -ForegroundColor Gray
    }
}

if ($billId) {
    Test-API "删除水电账单" {
        $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/$billId" -Method DELETE -Headers $headers
        Write-Host "  已删除水电账单: $billId" -ForegroundColor Gray
    }
}

# ============================================
# 测试结果总结
# ============================================
Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "  测试结果总结" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "总测试数: $($testResults.Total)" -ForegroundColor White
Write-Host "通过: $($testResults.Passed)" -ForegroundColor Green
Write-Host "失败: $($testResults.Failed)" -ForegroundColor $(if ($testResults.Failed -eq 0) { "Green" } else { "Red" })
Write-Host "成功率: $([Math]::Round($testResults.Passed / $testResults.Total * 100, 2))%" -ForegroundColor $(if ($testResults.Failed -eq 0) { "Green" } else { "Yellow" })

if ($testResults.Errors.Count -gt 0) {
    Write-Host "`n失败详情:" -ForegroundColor Red
    foreach ($error in $testResults.Errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
}

Write-Host "`nTest completed!" -ForegroundColor Green
