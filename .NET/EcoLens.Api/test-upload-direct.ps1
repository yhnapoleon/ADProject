# Direct file upload test using Invoke-WebRequest with proper form data

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,
    
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Direct File Upload Test" -ForegroundColor Green
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

# Step 2: Upload File
Write-Host "`n=== Step 2: Upload Bill File ===" -ForegroundColor Cyan

try {
    Write-Host "Uploading file..." -ForegroundColor Yellow
    
    # Use Invoke-WebRequest with -InFile parameter (simplest method)
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    $form = @{
        file = Get-Item $FilePath
    }
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/UtilityBill/upload" -Method POST -Headers $headers -Form $form
    
    if ($response.StatusCode -eq 200) {
        $responseJson = $response.Content | ConvertFrom-Json
        
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
        
        # Verify against expected values
        Write-Host "`n=== Verification ===" -ForegroundColor Cyan
        $expectedElectricity = 517.0
        $expectedWater = 8.2
        $expectedGas = 0.0
        
        if ($responseJson.electricityUsage -and [math]::Abs($responseJson.electricityUsage - $expectedElectricity) -lt 1) {
            Write-Host "✓ Electricity usage matches expected: $expectedElectricity kWh" -ForegroundColor Green
        } else {
            Write-Host "⚠ Electricity usage: Expected ~$expectedElectricity kWh, Got $($responseJson.electricityUsage)" -ForegroundColor Yellow
        }
        
        if ($responseJson.waterUsage -and [math]::Abs($responseJson.waterUsage - $expectedWater) -lt 0.1) {
            Write-Host "✓ Water usage matches expected: $expectedWater m³" -ForegroundColor Green
        } else {
            Write-Host "⚠ Water usage: Expected ~$expectedWater m³, Got $($responseJson.waterUsage)" -ForegroundColor Yellow
        }
        
        if ($responseJson.gasUsage -eq $expectedGas) {
            Write-Host "✓ Gas usage matches expected: $expectedGas" -ForegroundColor Green
        } else {
            Write-Host "⚠ Gas usage: Expected $expectedGas, Got $($responseJson.gasUsage)" -ForegroundColor Yellow
        }
        
        Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    } else {
        Write-Host "✗ Upload failed: Status $($response.StatusCode)" -ForegroundColor Red
        Write-Host "Response: $($response.Content)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ Upload failed: $($_.Exception.Message)" -ForegroundColor Red
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
