# Test with Real Bill Data (Simulated OCR Result)
# This test uses the actual data from the provided bill image

$baseUrl = "http://localhost:5133"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Test with Real Bill Data" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Register and Login
Write-Host "`n=== Step 1: Register and Login ===" -ForegroundColor Cyan
$timestamp = Get-Date -Format 'HHmmss'
$username = "testuser$timestamp"
$email = "test$timestamp@example.com"

$registerBody = @{
    username = $username
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✓ Registered: $username ($email)" -ForegroundColor Green
} catch {
    Write-Host "⚠ Registration failed, trying login..." -ForegroundColor Yellow
}

$loginBody = @{
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Test Manual Input with Real Bill Data
Write-Host "`n=== Step 2: Create Bill with Real Data (Manual Input) ===" -ForegroundColor Cyan
Write-Host "Bill Period: 05 Nov 2025 - 05 Dec 2025" -ForegroundColor Gray
Write-Host "Electricity: 517 kWh" -ForegroundColor Gray
Write-Host "Water: 8.2 m³" -ForegroundColor Gray
Write-Host "Gas: 0 kWh" -ForegroundColor Gray

$billBody = @{
    billType = 3  # Combined bill
    billPeriodStart = "2025-11-05T00:00:00"
    billPeriodEnd = "2025-12-05T23:59:59"
    electricityUsage = 517.0
    waterUsage = 8.2
    gasUsage = 0.0
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/manual" -Method POST -Headers $headers -Body $billBody
    Write-Host "✓ Bill created successfully!" -ForegroundColor Green
    
    Write-Host "`n=== Results ===" -ForegroundColor Cyan
    Write-Host "Bill ID: $($response.id)" -ForegroundColor White
    Write-Host "Bill Type: $($response.billTypeName)" -ForegroundColor White
    Write-Host "Period: $($response.billPeriodStart) to $($response.billPeriodEnd)" -ForegroundColor White
    
    Write-Host "`n=== Usage Data ===" -ForegroundColor Cyan
    Write-Host "Electricity: $($response.electricityUsage) kWh" -ForegroundColor White
    Write-Host "Water: $($response.waterUsage) m³" -ForegroundColor White
    Write-Host "Gas: $($response.gasUsage)" -ForegroundColor White
    
    Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
    Write-Host "Electricity Carbon: $($response.electricityCarbonEmission) kg CO2" -ForegroundColor White
    Write-Host "Water Carbon: $($response.waterCarbonEmission) kg CO2" -ForegroundColor White
    Write-Host "Gas Carbon: $($response.gasCarbonEmission) kg CO2" -ForegroundColor White
    Write-Host "Total Carbon: $($response.totalCarbonEmission) kg CO2" -ForegroundColor Green
    
    # Verify calculations
    Write-Host "`n=== Calculation Verification ===" -ForegroundColor Cyan
    $expectedElectricity = [math]::Round(517.0 * 0.4057, 4)
    $expectedWater = [math]::Round(8.2 * 0.419, 4)
    $expectedGas = [math]::Round(0.0 * 0.184, 4)
    $expectedTotal = [math]::Round($expectedElectricity + $expectedWater + $expectedGas, 4)
    
    Write-Host "Expected Electricity Carbon: $expectedElectricity kg CO2" -ForegroundColor Gray
    Write-Host "Actual Electricity Carbon: $([math]::Round($response.electricityCarbonEmission, 4)) kg CO2" -ForegroundColor Gray
    Write-Host "Expected Water Carbon: $expectedWater kg CO2" -ForegroundColor Gray
    Write-Host "Actual Water Carbon: $([math]::Round($response.waterCarbonEmission, 4)) kg CO2" -ForegroundColor Gray
    Write-Host "Expected Total Carbon: $expectedTotal kg CO2" -ForegroundColor Gray
    Write-Host "Actual Total Carbon: $([math]::Round($response.totalCarbonEmission, 4)) kg CO2" -ForegroundColor Gray
    
    $electricityMatch = [math]::Abs([math]::Round($response.electricityCarbonEmission, 4) - $expectedElectricity) -lt 0.0001
    $waterMatch = [math]::Abs([math]::Round($response.waterCarbonEmission, 4) - $expectedWater) -lt 0.0001
    $totalMatch = [math]::Abs([math]::Round($response.totalCarbonEmission, 4) - $expectedTotal) -lt 0.0001
    
    if ($electricityMatch -and $waterMatch -and $totalMatch) {
        Write-Host "✓ All calculations are correct!" -ForegroundColor Green
    } else {
        Write-Host "✗ Some calculations are incorrect" -ForegroundColor Red
    }
    
    Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    Write-Host "Note: This test used manual input. For OCR testing, you need to upload the actual image file." -ForegroundColor Yellow
    
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response: $responseBody" -ForegroundColor Yellow
        } catch {
            # Ignore
        }
    }
}
