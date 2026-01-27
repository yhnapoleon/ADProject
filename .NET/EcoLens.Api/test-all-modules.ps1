# 全面模块测试脚本 - 检查所有功能模块状态
$baseUrl = "http://localhost:5133"
$testResults = @{}

function Test-Module {
    param(
        [string]$ModuleName,
        [string]$Endpoint,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [string]$ExpectedField = $null
    )
    
    try {
        $params = @{
            Uri = "$baseUrl$Endpoint"
            Method = $Method
            Headers = $Headers
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json)
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params
        
        if ($ExpectedField -and -not $response.$ExpectedField) {
            throw "响应中缺少字段: $ExpectedField"
        }
        
        return @{ Success = $true; Response = $response; Error = $null }
    } catch {
        $errorMsg = $_.Exception.Message
        if ($_.Exception.Response) {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            $errorMsg += " | Response: $responseBody"
        }
        return @{ Success = $false; Response = $null; Error = $errorMsg }
    }
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "  全面模块状态检查 - EcoLens API" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# 获取Token
Write-Host "=== 获取认证Token ===" -ForegroundColor Cyan
$timestamp = Get-Date -Format 'yyyyMMddHHmmss'
$username = "testuser$timestamp"
$email = "test$timestamp@example.com"
$password = "Test123!"
$token = ""

try {
    $registerBody = @{ username = $username; email = $email; password = $password } | ConvertTo-Json
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    $token = $registerResponse.token
    Write-Host "✓ Token获取成功 (长度: $($token.Length))" -ForegroundColor Green
} catch {
    Write-Host "✗ Token获取失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 测试所有模块
$modules = @(
    @{ Name = "用户认证 - 登录"; Endpoint = "/api/auth/login"; Method = "POST"; Body = @{ email = $email; password = $password }; ExpectedField = "token" },
    @{ Name = "用户资料 - 获取当前用户"; Endpoint = "/api/user/profile"; Method = "GET"; ExpectedField = "id" },
    @{ Name = "出行记录 - 预览路线"; Endpoint = "/api/travel/preview"; Method = "POST"; Body = @{ originAddress = "Beijing"; destinationAddress = "Shanghai"; transportMode = 3 }; ExpectedField = "distanceKilometers" },
    @{ Name = "出行记录 - 获取统计"; Endpoint = "/api/travel/statistics"; Method = "GET"; ExpectedField = "totalCount" },
    @{ Name = "出行记录 - 获取列表"; Endpoint = "/api/travel/my-travels"; Method = "GET"; ExpectedField = "items" },
    @{ Name = "水电账单 - 获取统计"; Endpoint = "/api/UtilityBill/statistics"; Method = "GET"; ExpectedField = "totalCount" },
    @{ Name = "水电账单 - 获取列表"; Endpoint = "/api/UtilityBill/my-bills"; Method = "GET"; ExpectedField = "items" },
    @{ Name = "活动记录 - 获取统计"; Endpoint = "/api/Activity/statistics"; Method = "GET"; ExpectedField = "totalEmission" },
    @{ Name = "活动记录 - 获取列表"; Endpoint = "/api/Activity/logs"; Method = "GET"; ExpectedField = "items" },
    @{ Name = "活动记录 - 获取图表数据"; Endpoint = "/api/Activity/chart-data"; Method = "GET"; ExpectedField = "data" },
    @{ Name = "活动记录 - 获取热力图"; Endpoint = "/api/Activity/heatmap"; Method = "GET" },
    @{ Name = "碳排放因子 - 获取列表"; Endpoint = "/api/carbon/factors"; Method = "GET"; ExpectedField = "Count" }
    @{ Name = "排行榜 - 获取列表"; Endpoint = "/api/leaderboard"; Method = "GET"; ExpectedField = "Count" },
    @{ Name = "社区 - 获取帖子列表"; Endpoint = "/api/community/posts"; Method = "GET"; ExpectedField = "items" },
    @{ Name = "AI洞察 - 获取列表"; Endpoint = "/api/insight"; Method = "GET"; ExpectedField = "Count" },
    @{ Name = "步数记录 - 获取统计"; Endpoint = "/api/step/statistics"; Method = "GET" },
    @{ Name = "条形码 - 获取列表"; Endpoint = "/api/Barcode"; Method = "GET"; ExpectedField = "Count" }
)

Write-Host "`n=== 开始测试各模块 ===" -ForegroundColor Cyan
$moduleResults = @{}

foreach ($module in $modules) {
    Write-Host "`n测试: $($module.Name)" -ForegroundColor Yellow
    $result = Test-Module -ModuleName $module.Name -Endpoint $module.Endpoint -Method $module.Method -Headers $headers -Body $module.Body -ExpectedField $module.ExpectedField
    
    if ($result.Success) {
        Write-Host "  ✓ 通过" -ForegroundColor Green
        $moduleResults[$module.Name] = "正常"
    } else {
        Write-Host "  ✗ 失败: $($result.Error)" -ForegroundColor Red
        $moduleResults[$module.Name] = "失败: $($result.Error)"
    }
}

# 输出总结
Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "  模块状态总结" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

$normalCount = 0
$failedCount = 0

foreach ($key in $moduleResults.Keys | Sort-Object) {
    if ($moduleResults[$key] -eq "正常") {
        Write-Host "✓ $key : $($moduleResults[$key])" -ForegroundColor Green
        $normalCount++
    } else {
        Write-Host "✗ $key : $($moduleResults[$key])" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host "`n总计: $($moduleResults.Count) 个模块" -ForegroundColor Cyan
Write-Host "正常: $normalCount" -ForegroundColor Green
Write-Host "失败: $failedCount" -ForegroundColor Red
Write-Host "成功率: $([math]::Round($normalCount / $moduleResults.Count * 100, 2))%" -ForegroundColor $(if ($failedCount -eq 0) { "Green" } else { "Yellow" })
