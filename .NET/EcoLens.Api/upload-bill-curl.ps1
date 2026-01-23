# Upload Utility Bill using curl (more reliable)

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,
    
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Upload Utility Bill Test (curl)" -ForegroundColor Green
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

# Step 2: Upload File using curl
Write-Host "`n=== Step 2: Upload Bill File ===" -ForegroundColor Cyan

try {
    Write-Host "Uploading file using curl..." -ForegroundColor Yellow
    
    # Use curl to upload file
    $curlCommand = "curl -X POST `"$BaseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@$FilePath`""
    
    $response = Invoke-Expression $curlCommand
    
    if ($LASTEXITCODE -eq 0) {
        $responseJson = $response | ConvertFrom-Json
        
        Write-Host "✓ Upload successful!" -ForegroundColor Green
        Write-Host "`n=== Extracted Data ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($responseJson.id)" -ForegroundColor White
        Write-Host "Bill Type: $($responseJson.billTypeName)" -ForegroundColor White
        Write-Host "Period: $($responseJson.billPeriodStart) to $($responseJson.billPeriodEnd)" -ForegroundColor White
        Write-Host "Input Method: $($responseJson.inputMethodName)" -ForegroundColor White
        if ($responseJson.ocrConfidence) {
            Write-Host "OCR Confidence: $([math]::Round($responseJson.ocrConfidence * 100, 2))%" -ForegroundColor White
        }
        
        Write-Host "`n=== Usage Data ===" -ForegroundColor Cyan
        if ($responseJson.electricityUsage) {
            Write-Host "Electricity: $($responseJson.electricityUsage) kWh" -ForegroundColor White
        }
        if ($responseJson.waterUsage) {
            Write-Host "Water: $($responseJson.waterUsage) m³" -ForegroundColor White
        }
        if ($responseJson.gasUsage) {
            Write-Host "Gas: $($responseJson.gasUsage)" -ForegroundColor White
        }
        
        Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
        Write-Host "Electricity Carbon: $($responseJson.electricityCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Water Carbon: $($responseJson.waterCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Gas Carbon: $($responseJson.gasCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Total Carbon: $($responseJson.totalCarbonEmission) kg CO2" -ForegroundColor Green
        
        Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    } else {
        Write-Host "✗ Upload failed" -ForegroundColor Red
        Write-Host "Response: $response" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Trying alternative method..." -ForegroundColor Yellow
    
    # Alternative: Use Invoke-WebRequest
    try {
        $boundary = [System.Guid]::NewGuid().ToString()
        $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
        $fileName = [System.IO.Path]::GetFileName($FilePath)
        
        $bodyLines = @(
            "--$boundary",
            "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"",
            "Content-Type: image/jpeg",
            "",
            ""
        )
        
        $headerText = $bodyLines -join "`r`n"
        $footerText = "`r`n--$boundary--`r`n"
        
        $headerBytes = [System.Text.Encoding]::UTF8.GetBytes($headerText)
        $footerBytes = [System.Text.Encoding]::UTF8.GetBytes($footerText)
        
        $bodyBytes = $headerBytes + $fileBytes + $footerBytes
        
        $headers = @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "multipart/form-data; boundary=$boundary"
        }
        
        $response = Invoke-WebRequest -Uri "$BaseUrl/api/UtilityBill/upload" -Method POST -Headers $headers -Body $bodyBytes -ContentType "multipart/form-data; boundary=$boundary"
        
        $responseJson = $response.Content | ConvertFrom-Json
        
        Write-Host "✓ Upload successful (alternative method)!" -ForegroundColor Green
        Write-Host "`n=== Extracted Data ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($responseJson.id)" -ForegroundColor White
        Write-Host "Bill Type: $($responseJson.billTypeName)" -ForegroundColor White
        Write-Host "Period: $($responseJson.billPeriodStart) to $($responseJson.billPeriodEnd)" -ForegroundColor White
        Write-Host "Input Method: $($responseJson.inputMethodName)" -ForegroundColor White
        if ($responseJson.ocrConfidence) {
            Write-Host "OCR Confidence: $([math]::Round($responseJson.ocrConfidence * 100, 2))%" -ForegroundColor White
        }
        
        Write-Host "`n=== Usage Data ===" -ForegroundColor Cyan
        if ($responseJson.electricityUsage) {
            Write-Host "Electricity: $($responseJson.electricityUsage) kWh" -ForegroundColor White
        }
        if ($responseJson.waterUsage) {
            Write-Host "Water: $($responseJson.waterUsage) m³" -ForegroundColor White
        }
        if ($responseJson.gasUsage) {
            Write-Host "Gas: $($responseJson.gasUsage)" -ForegroundColor White
        }
        
        Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
        Write-Host "Electricity Carbon: $($responseJson.electricityCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Water Carbon: $($responseJson.waterCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Gas Carbon: $($responseJson.gasCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Total Carbon: $($responseJson.totalCarbonEmission) kg CO2" -ForegroundColor Green
        
        Write-Host "`n=== Test Complete ===" -ForegroundColor Green
        
    } catch {
        Write-Host "✗ Alternative method also failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                Write-Host "Response: $responseBody" -ForegroundColor Yellow
            } catch {
                # Ignore
            }
        }
        exit 1
    }
}
