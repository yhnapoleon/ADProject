# Final upload test using proper multipart form-data construction

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,
    
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Final Upload Test" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

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
    $null = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
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

# Step 2: Upload File using proper multipart construction
Write-Host "`n=== Step 2: Upload Bill File ===" -ForegroundColor Cyan

try {
    $fileName = [System.IO.Path]::GetFileName($FilePath)
    $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
    $boundary = [System.Guid]::NewGuid().ToString()
    
    # Build multipart form data manually
    $CRLF = "`r`n"
    $bodyParts = @()
    
    # Header
    $bodyParts += "--$boundary"
    $bodyParts += "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`""
    $bodyParts += "Content-Type: image/jpeg"
    $bodyParts += ""
    
    # Convert header to bytes
    $headerText = ($bodyParts -join $CRLF) + $CRLF
    $headerBytes = [System.Text.Encoding]::UTF8.GetBytes($headerText)
    
    # Footer
    $footerText = $CRLF + "--$boundary--" + $CRLF
    $footerBytes = [System.Text.Encoding]::UTF8.GetBytes($footerText)
    
    # Combine
    $bodyBytes = $headerBytes + $fileBytes + $footerBytes
    
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }
    
    Write-Host "Uploading file (size: $($bodyBytes.Length) bytes)..." -ForegroundColor Yellow
    
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
    
    # Verify against expected values from the bill
    Write-Host "`n=== Verification ===" -ForegroundColor Cyan
    $expectedElectricity = 517.0
    $expectedWater = 8.2
    $expectedGas = 0.0
    
    $electricityMatch = $false
    $waterMatch = $false
    $gasMatch = $false
    
    if ($response.electricityUsage) {
        $diff = [math]::Abs($response.electricityUsage - $expectedElectricity)
        if ($diff -lt 5) {  # Allow 5 kWh tolerance
            Write-Host "✓ Electricity usage is close to expected: $expectedElectricity kWh (Got: $($response.electricityUsage))" -ForegroundColor Green
            $electricityMatch = $true
        } else {
            Write-Host "⚠ Electricity usage: Expected ~$expectedElectricity kWh, Got $($response.electricityUsage)" -ForegroundColor Yellow
        }
    }
    
    if ($response.waterUsage) {
        $diff = [math]::Abs($response.waterUsage - $expectedWater)
        if ($diff -lt 0.5) {  # Allow 0.5 m³ tolerance
            Write-Host "✓ Water usage is close to expected: $expectedWater m³ (Got: $($response.waterUsage))" -ForegroundColor Green
            $waterMatch = $true
        } else {
            Write-Host "⚠ Water usage: Expected ~$expectedWater m³, Got $($response.waterUsage)" -ForegroundColor Yellow
        }
    }
    
    if ($response.gasUsage -eq $expectedGas) {
        Write-Host "✓ Gas usage matches expected: $expectedGas" -ForegroundColor Green
        $gasMatch = $true
    } else {
        Write-Host "⚠ Gas usage: Expected $expectedGas, Got $($response.gasUsage)" -ForegroundColor Yellow
    }
    
    if ($electricityMatch -and $waterMatch -and $gasMatch) {
        Write-Host "`n✓✓✓ All extracted values match expected values!" -ForegroundColor Green
    } else {
        Write-Host "`n⚠ Some values don't match. This might be due to:" -ForegroundColor Yellow
        Write-Host "  - OCR text recognition accuracy" -ForegroundColor Gray
        Write-Host "  - Data extraction pattern matching" -ForegroundColor Gray
        Write-Host "  - Bill format variations" -ForegroundColor Gray
    }
    
    Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response: $responseBody" -ForegroundColor Yellow
        } catch {
            # Ignore
        }
    }
    exit 1
}
