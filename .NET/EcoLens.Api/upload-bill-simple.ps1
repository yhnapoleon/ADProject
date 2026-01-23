# Simple Utility Bill Upload Test Script
# Uses .NET HttpClient for reliable file upload

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,
    
    [string]$BaseUrl = "http://localhost:5133"
)

Add-Type -AssemblyName System.Net.Http

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Upload Utility Bill Test" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Check if file exists
if (-not (Test-Path $FilePath)) {
    Write-Host "Error: File not found: $FilePath" -ForegroundColor Red
    exit 1
}

$fileSize = (Get-Item $FilePath).Length
Write-Host "`nFile: $FilePath" -ForegroundColor Cyan
Write-Host "Size: $([math]::Round($fileSize/1KB, 2)) KB" -ForegroundColor Gray

# Step 1: Login
Write-Host "`n=== Step 1: Login ===" -ForegroundColor Cyan
$timestamp = Get-Date -Format 'HHmmss'
$email = "test$timestamp@example.com"

$registerBody = @{
    username = "testuser$timestamp"
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✓ Registered new user" -ForegroundColor Green
} catch {
    Write-Host "⚠ Registration failed (user may exist)" -ForegroundColor Yellow
}

$loginBody = @{
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Upload File using HttpClient
Write-Host "`n=== Step 2: Upload Bill File ===" -ForegroundColor Cyan

try {
    $httpClient = New-Object System.Net.Http.HttpClient
    $httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer $token")
    
    $fileName = [System.IO.Path]::GetFileName($FilePath)
    $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
    $fileContent = New-Object System.Net.Http.ByteArrayContent($fileBytes)
    
    $multipartContent = New-Object System.Net.Http.MultipartFormDataContent
    $fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg")
    $multipartContent.Add($fileContent, "file", $fileName)
    
    Write-Host "Uploading file..." -ForegroundColor Yellow
    
    $response = $httpClient.PostAsync("$BaseUrl/api/UtilityBill/upload", $multipartContent).Result
    
    if ($response.IsSuccessStatusCode) {
        $responseContent = $response.Content.ReadAsStringAsync().Result | ConvertFrom-Json
        
        Write-Host "✓ Upload successful!" -ForegroundColor Green
        Write-Host "`n=== Extracted Data ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($responseContent.id)" -ForegroundColor White
        Write-Host "Bill Type: $($responseContent.billTypeName)" -ForegroundColor White
        Write-Host "Period: $($responseContent.billPeriodStart) to $($responseContent.billPeriodEnd)" -ForegroundColor White
        Write-Host "Input Method: $($responseContent.inputMethodName)" -ForegroundColor White
        if ($responseContent.ocrConfidence) {
            Write-Host "OCR Confidence: $([math]::Round($responseContent.ocrConfidence * 100, 2))%" -ForegroundColor White
        }
        
        Write-Host "`n=== Usage Data ===" -ForegroundColor Cyan
        if ($responseContent.electricityUsage) {
            Write-Host "Electricity: $($responseContent.electricityUsage) kWh" -ForegroundColor White
        }
        if ($responseContent.waterUsage) {
            Write-Host "Water: $($responseContent.waterUsage) m³" -ForegroundColor White
        }
        if ($responseContent.gasUsage) {
            Write-Host "Gas: $($responseContent.gasUsage)" -ForegroundColor White
        }
        
        Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
        Write-Host "Electricity Carbon: $($responseContent.electricityCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Water Carbon: $($responseContent.waterCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Gas Carbon: $($responseContent.gasCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Total Carbon: $($responseContent.totalCarbonEmission) kg CO2" -ForegroundColor Green
        
        Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    } else {
        $errorContent = $response.Content.ReadAsStringAsync().Result
        Write-Host "✗ Upload failed: $($response.StatusCode)" -ForegroundColor Red
        Write-Host "Response: $errorContent" -ForegroundColor Yellow
    }
    
    $httpClient.Dispose()
    
} catch {
    Write-Host "✗ Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "Inner Exception: $($_.Exception.InnerException.Message)" -ForegroundColor Yellow
    }
    exit 1
}
