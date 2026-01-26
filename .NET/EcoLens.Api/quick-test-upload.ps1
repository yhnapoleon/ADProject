# Quick Upload Test - Simple and Reliable
param(
    [string]$FilePath = "E:\OneDrive\Desktop\AD\Test.jpg",
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n=== Quick Upload Test ===" -ForegroundColor Green
Write-Host "File: $FilePath" -ForegroundColor Cyan

# Check if project is running
try {
    $healthCheck = Invoke-WebRequest -Uri "$BaseUrl/swagger" -Method GET -TimeoutSec 2 -ErrorAction Stop
    Write-Host "✓ Project is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Project is not running. Please start it with: dotnet run" -ForegroundColor Red
    Write-Host "`nThen run this script again." -ForegroundColor Yellow
    exit 1
}

# Login
Write-Host "`n=== Step 1: Login ===" -ForegroundColor Cyan
$timestamp = Get-Date -Format 'HHmmss'
$email = "test$timestamp@example.com"

try {
    $registerBody = @{ username = "testuser$timestamp"; email = $email; password = "Test123!" } | ConvertTo-Json
    $null = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✓ Registered" -ForegroundColor Green
} catch { Write-Host "⚠ Registration skipped" -ForegroundColor Yellow }

$loginBody = @{ email = $email; password = "Test123!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "✓ Logged in" -ForegroundColor Green

# Upload using .NET HttpClient (most reliable)
Write-Host "`n=== Step 2: Upload File ===" -ForegroundColor Cyan

Add-Type -AssemblyName System.Net.Http

try {
    $httpClient = New-Object System.Net.Http.HttpClient
    $httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer $token")
    
    $fileName = [System.IO.Path]::GetFileName($FilePath)
    $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
    $fileContent = New-Object System.Net.Http.ByteArrayContent(,$fileBytes)
    $fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg")
    
    $multipart = New-Object System.Net.Http.MultipartFormDataContent
    $multipart.Add($fileContent, "file", $fileName)
    
    Write-Host "Uploading..." -ForegroundColor Yellow
    $response = $httpClient.PostAsync("$BaseUrl/api/UtilityBill/upload", $multipart).Result
    
    if ($response.IsSuccessStatusCode) {
        $content = $response.Content.ReadAsStringAsync().Result
        $json = $content | ConvertFrom-Json
        
        Write-Host "✓ Upload successful!" -ForegroundColor Green
        Write-Host "`n=== Results ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($json.id)" -ForegroundColor White
        Write-Host "Type: $($json.billTypeName)" -ForegroundColor White
        Write-Host "Period: $($json.billPeriodStart) to $($json.billPeriodEnd)" -ForegroundColor White
        Write-Host "Method: $($json.inputMethodName)" -ForegroundColor White
        if ($json.ocrConfidence) {
            Write-Host "OCR Confidence: $([math]::Round($json.ocrConfidence * 100, 2))%" -ForegroundColor White
        }
        
        Write-Host "`n=== Usage ===" -ForegroundColor Cyan
        if ($json.electricityUsage) { Write-Host "Electricity: $($json.electricityUsage) kWh" -ForegroundColor White }
        if ($json.waterUsage) { Write-Host "Water: $($json.waterUsage) m³" -ForegroundColor White }
        if ($json.gasUsage) { Write-Host "Gas: $($json.gasUsage)" -ForegroundColor White }
        
        Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
        Write-Host "Electricity: $($json.electricityCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Water: $($json.waterCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Gas: $($json.gasCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Total: $($json.totalCarbonEmission) kg CO2" -ForegroundColor Green
        
        Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    } else {
        $errorContent = $response.Content.ReadAsStringAsync().Result
        Write-Host "✗ Upload failed: $($response.StatusCode)" -ForegroundColor Red
        Write-Host "Response: $errorContent" -ForegroundColor Yellow
    }
    
    $httpClient.Dispose()
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "Inner: $($_.Exception.InnerException.Message)" -ForegroundColor Yellow
    }
}
