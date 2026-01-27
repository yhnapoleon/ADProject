# Simple Utility Bill Upload Test Script
# Usage: .\upload-bill-test.ps1 -FilePath "path\to\your\bill.jpg"

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,
    
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Upload Utility Bill Test" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Check if file exists
if (-not (Test-Path $FilePath)) {
    Write-Host "Error: File not found: $FilePath" -ForegroundColor Red
    exit 1
}

# Check file size (max 10MB)
$fileSize = (Get-Item $FilePath).Length
if ($fileSize -gt 10MB) {
    Write-Host "Error: File size exceeds 10MB limit (Current: $([math]::Round($fileSize/1MB, 2)) MB)" -ForegroundColor Red
    exit 1
}

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
    Write-Host "⚠ Registration failed (user may exist), trying login..." -ForegroundColor Yellow
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

# Step 2: Upload File
Write-Host "`n=== Step 2: Upload Bill File ===" -ForegroundColor Cyan

try {
    # Read file bytes
    $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
    $fileName = [System.IO.Path]::GetFileName($FilePath)
    
    # Create multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $bodyLines = New-Object System.Collections.ArrayList
    $bodyLines.Add("--$boundary") | Out-Null
    $bodyLines.Add("Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"") | Out-Null
    $bodyLines.Add("Content-Type: application/octet-stream") | Out-Null
    $bodyLines.Add("") | Out-Null
    
    # Convert to byte array
    $headerBytes = [System.Text.Encoding]::UTF8.GetBytes(($bodyLines -join $LF) + $LF)
    $footerBytes = [System.Text.Encoding]::UTF8.GetBytes($LF + "--$boundary--" + $LF)
    
    $bodyBytes = $headerBytes + $fileBytes + $footerBytes
    
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }
    
    Write-Host "Uploading file..." -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/upload" -Method POST -Headers $headers -Body $bodyBytes
    
    Write-Host "✓ Upload successful!" -ForegroundColor Green
    Write-Host "`n=== Extracted Data ===" -ForegroundColor Cyan
    Write-Host "Bill ID: $($response.id)" -ForegroundColor White
    Write-Host "Bill Type: $($response.billTypeName)" -ForegroundColor White
    Write-Host "Period: $($response.billPeriodStart) to $($response.billPeriodEnd)" -ForegroundColor White
    Write-Host "Input Method: $($response.inputMethodName)" -ForegroundColor White
    if ($response.ocrConfidence) {
        Write-Host "OCR Confidence: $([math]::Round($response.ocrConfidence * 100, 2))%" -ForegroundColor White
    }
    
    Write-Host "`n=== Usage Data ===" -ForegroundColor Cyan
    if ($response.electricityUsage) {
        Write-Host "Electricity: $($response.electricityUsage) kWh" -ForegroundColor White
    }
    if ($response.waterUsage) {
        Write-Host "Water: $($response.waterUsage) m³" -ForegroundColor White
    }
    if ($response.gasUsage) {
        Write-Host "Gas: $($response.gasUsage)" -ForegroundColor White
    }
    
    Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
    Write-Host "Electricity Carbon: $($response.electricityCarbonEmission) kg CO2" -ForegroundColor White
    Write-Host "Water Carbon: $($response.waterCarbonEmission) kg CO2" -ForegroundColor White
    Write-Host "Gas Carbon: $($response.gasCarbonEmission) kg CO2" -ForegroundColor White
    Write-Host "Total Carbon: $($response.totalCarbonEmission) kg CO2" -ForegroundColor Green
    
    Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response: $responseBody" -ForegroundColor Yellow
        } catch {
            # Ignore stream reading errors
        }
    }
    exit 1
}
