# Utility Bills API Test Script
$baseUrl = "http://localhost:5133"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Utility Bills API Test" -ForegroundColor Green
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
    Write-Host "✗ Registration failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$loginBody = @{
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful. Token obtained (length: $($token.Length))" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Test Manual Creation - Electricity Bill
Write-Host "`n=== Step 2: Create Electricity Bill (Manual) ===" -ForegroundColor Cyan
$electricityBody = @{
    billType = 0
    billPeriodStart = "2024-01-01T00:00:00"
    billPeriodEnd = "2024-01-31T23:59:59"
    electricityUsage = 150.5
    waterUsage = $null
    gasUsage = $null
} | ConvertTo-Json

$electricityBillId = $null
try {
    $electricityResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/manual" -Method POST -Headers $headers -Body $electricityBody
    $electricityBillId = $electricityResponse.id
    Write-Host "✓ Electricity bill created! ID: $electricityBillId" -ForegroundColor Green
    Write-Host "  Bill Type: $($electricityResponse.billTypeName)"
    Write-Host "  Electricity Usage: $($electricityResponse.electricityUsage) kWh"
    Write-Host "  Electricity Carbon: $($electricityResponse.electricityCarbonEmission) kg CO2"
    Write-Host "  Total Carbon: $($electricityResponse.totalCarbonEmission) kg CO2"
    
    # Verify calculation: 150.5 * 0.4057 = 61.05785
    $expectedCarbon = [math]::Round(150.5 * 0.4057, 4)
    $actualCarbon = [math]::Round($electricityResponse.electricityCarbonEmission, 4)
    if ([math]::Abs($actualCarbon - $expectedCarbon) -lt 0.0001) {
        Write-Host "  ✓ Carbon calculation correct: $actualCarbon kg CO2" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Carbon calculation incorrect: Expected $expectedCarbon, Got $actualCarbon" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Create electricity bill failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
}

# Step 3: Test Manual Creation - Water Bill
Write-Host "`n=== Step 3: Create Water Bill (Manual) ===" -ForegroundColor Cyan
$waterBody = @{
    billType = 1
    billPeriodStart = "2024-01-01T00:00:00"
    billPeriodEnd = "2024-01-31T23:59:59"
    electricityUsage = $null
    waterUsage = 12.5
    gasUsage = $null
} | ConvertTo-Json

$waterBillId = $null
try {
    $waterResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/manual" -Method POST -Headers $headers -Body $waterBody
    $waterBillId = $waterResponse.id
    Write-Host "✓ Water bill created! ID: $waterBillId" -ForegroundColor Green
    Write-Host "  Bill Type: $($waterResponse.billTypeName)"
    Write-Host "  Water Usage: $($waterResponse.waterUsage) m³"
    Write-Host "  Water Carbon: $($waterResponse.waterCarbonEmission) kg CO2"
    Write-Host "  Total Carbon: $($waterResponse.totalCarbonEmission) kg CO2"
    
    # Verify calculation: 12.5 * 0.419 = 5.2375
    $expectedCarbon = [math]::Round(12.5 * 0.419, 4)
    $actualCarbon = [math]::Round($waterResponse.waterCarbonEmission, 4)
    if ([math]::Abs($actualCarbon - $expectedCarbon) -lt 0.0001) {
        Write-Host "  ✓ Carbon calculation correct: $actualCarbon kg CO2" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Carbon calculation incorrect: Expected $expectedCarbon, Got $actualCarbon" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Create water bill failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
}

# Step 4: Test Manual Creation - Combined Bill
Write-Host "`n=== Step 4: Create Combined Bill (Manual) ===" -ForegroundColor Cyan
$combinedBody = @{
    billType = 3
    billPeriodStart = "2024-02-01T00:00:00"
    billPeriodEnd = "2024-02-29T23:59:59"
    electricityUsage = 150.0
    waterUsage = 12.5
    gasUsage = 50.0
} | ConvertTo-Json

$combinedBillId = $null
try {
    $combinedResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/manual" -Method POST -Headers $headers -Body $combinedBody
    $combinedBillId = $combinedResponse.id
    Write-Host "✓ Combined bill created! ID: $combinedBillId" -ForegroundColor Green
    Write-Host "  Bill Type: $($combinedResponse.billTypeName)"
    Write-Host "  Electricity: $($combinedResponse.electricityUsage) kWh -> $($combinedResponse.electricityCarbonEmission) kg CO2"
    Write-Host "  Water: $($combinedResponse.waterUsage) m³ -> $($combinedResponse.waterCarbonEmission) kg CO2"
    Write-Host "  Gas: $($combinedResponse.gasUsage) -> $($combinedResponse.gasCarbonEmission) kg CO2"
    Write-Host "  Total Carbon: $($combinedResponse.totalCarbonEmission) kg CO2"
    
    # Verify calculation
    $expectedElectricity = [math]::Round(150.0 * 0.4057, 4)
    $expectedWater = [math]::Round(12.5 * 0.419, 4)
    $expectedGas = [math]::Round(50.0 * 0.184, 4)
    $expectedTotal = [math]::Round($expectedElectricity + $expectedWater + $expectedGas, 4)
    $actualTotal = [math]::Round($combinedResponse.totalCarbonEmission, 4)
    
    if ([math]::Abs($actualTotal - $expectedTotal) -lt 0.0001) {
        Write-Host "  ✓ Total carbon calculation correct: $actualTotal kg CO2" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Total carbon calculation incorrect: Expected $expectedTotal, Got $actualTotal" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Create combined bill failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
}

# Step 5: Get Bills List
Write-Host "`n=== Step 5: Get Bills List ===" -ForegroundColor Cyan
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/my-bills?page=1&pageSize=10" -Method GET -Headers $headers
    Write-Host "✓ List retrieved!" -ForegroundColor Green
    Write-Host "  Total Count: $($listResponse.totalCount)"
    Write-Host "  Page: $($listResponse.page)"
    Write-Host "  Page Size: $($listResponse.pageSize)"
    Write-Host "  Total Pages: $($listResponse.totalPages)"
    Write-Host "  Items Count: $($listResponse.items.Count)"
    
    if ($listResponse.items.Count -gt 0) {
        Write-Host "  First Bill: ID=$($listResponse.items[0].id), Type=$($listResponse.items[0].billTypeName), Carbon=$($listResponse.items[0].totalCarbonEmission) kg CO2"
    }
} catch {
    Write-Host "✗ List failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
}

# Step 6: Get Bill by ID
if ($electricityBillId) {
    Write-Host "`n=== Step 6: Get Bill by ID ===" -ForegroundColor Cyan
    try {
        $singleResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/$electricityBillId" -Method GET -Headers $headers
        Write-Host "✓ Single bill retrieved! ID: $($singleResponse.id)" -ForegroundColor Green
        Write-Host "  Bill Type: $($singleResponse.billTypeName)"
        Write-Host "  Period: $($singleResponse.billPeriodStart) to $($singleResponse.billPeriodEnd)"
        Write-Host "  Input Method: $($singleResponse.inputMethodName)"
        Write-Host "  Total Carbon: $($singleResponse.totalCarbonEmission) kg CO2"
    } catch {
        Write-Host "✗ Get single failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 7: Test Filtering by Bill Type
Write-Host "`n=== Step 7: Filter by Bill Type ===" -ForegroundColor Cyan
try {
    $filterResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/my-bills?billType=0&page=1&pageSize=10" -Method GET -Headers $headers
    Write-Host "✓ Filtered list retrieved!" -ForegroundColor Green
    Write-Host "  Filtered Count: $($filterResponse.totalCount)"
    Write-Host "  Items Count: $($filterResponse.items.Count)"
    
    if ($filterResponse.items.Count -gt 0) {
        $allElectricity = $filterResponse.items | Where-Object { $_.billType -eq 0 }
        if ($allElectricity.Count -eq $filterResponse.items.Count) {
            Write-Host "  ✓ All items are electricity bills" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ Filtered $($filterResponse.items.Count) items, but $($allElectricity.Count) are electricity bills" -ForegroundColor Yellow
            Write-Host "    (This might be expected if billType filter is not working correctly)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "✗ Filter failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 8: Test Date Range Filtering
Write-Host "`n=== Step 8: Filter by Date Range ===" -ForegroundColor Cyan
try {
    $dateFilterResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/my-bills?startDate=2024-01-01&endDate=2024-01-31&page=1&pageSize=10" -Method GET -Headers $headers
    Write-Host "✓ Date filtered list retrieved!" -ForegroundColor Green
    Write-Host "  Filtered Count: $($dateFilterResponse.totalCount)"
    Write-Host "  Items Count: $($dateFilterResponse.items.Count)"
} catch {
    Write-Host "✗ Date filter failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 9: Get Statistics
Write-Host "`n=== Step 9: Get Statistics ===" -ForegroundColor Cyan
try {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/statistics" -Method GET -Headers $headers
    Write-Host "✓ Statistics retrieved!" -ForegroundColor Green
    Write-Host "  Total Records: $($statsResponse.totalRecords)"
    Write-Host "  Total Electricity Usage: $($statsResponse.totalElectricityUsage) kWh"
    Write-Host "  Total Water Usage: $($statsResponse.totalWaterUsage) m³"
    Write-Host "  Total Gas Usage: $($statsResponse.totalGasUsage)"
    Write-Host "  Total Carbon Emission: $($statsResponse.totalCarbonEmission) kg CO2"
    Write-Host "  By Bill Type: $($statsResponse.byBillType.Count) types"
    
    foreach ($typeStat in $statsResponse.byBillType) {
        Write-Host "    - $($typeStat.billTypeName): $($typeStat.recordCount) records, $($typeStat.totalCarbonEmission) kg CO2"
    }
} catch {
    Write-Host "✗ Statistics failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 10: Test Statistics with Date Range
Write-Host "`n=== Step 10: Get Statistics with Date Range ===" -ForegroundColor Cyan
try {
    $statsDateResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/statistics?startDate=2024-01-01&endDate=2024-01-31" -Method GET -Headers $headers
    Write-Host "✓ Statistics with date range retrieved!" -ForegroundColor Green
    Write-Host "  Total Records (Jan 2024): $($statsDateResponse.totalRecords)"
    Write-Host "  Total Carbon Emission: $($statsDateResponse.totalCarbonEmission) kg CO2"
} catch {
    Write-Host "✗ Statistics with date range failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 11: Test Input Validation - Invalid Date Range
Write-Host "`n=== Step 11: Test Input Validation (Invalid Date Range) ===" -ForegroundColor Cyan
try {
    $invalidBody = @{
        billType = 0
        billPeriodStart = "2024-01-31T00:00:00"
        billPeriodEnd = "2024-01-01T23:59:59"
        electricityUsage = 150.5
    } | ConvertTo-Json
    
    $invalidResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/manual" -Method POST -Headers $headers -Body $invalidBody
    Write-Host "✗ Should have failed but didn't" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✓ Correctly rejected invalid date range" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 12: Test Unauthorized Access
Write-Host "`n=== Step 12: Test Unauthorized Access ===" -ForegroundColor Cyan
try {
    $invalidHeaders = @{
        "Authorization" = "Bearer invalid_token"
        "Content-Type" = "application/json"
    }
    $unauthResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/my-bills" -Method GET -Headers $invalidHeaders
    Write-Host "✗ Should have failed but didn't" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ Correctly rejected unauthorized access" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 13: Test Get Non-existent Bill
Write-Host "`n=== Step 13: Test Get Non-existent Bill ===" -ForegroundColor Cyan
try {
    $notFoundResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/99999" -Method GET -Headers $headers
    Write-Host "✗ Should have failed but didn't" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✓ Correctly returned 404 for non-existent bill" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 14: Test Delete Bill
if ($waterBillId) {
    Write-Host "`n=== Step 14: Delete Bill ===" -ForegroundColor Cyan
    try {
        $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/$waterBillId" -Method DELETE -Headers $headers
        Write-Host "✓ Bill deleted successfully" -ForegroundColor Green
        
        # Verify deletion
        try {
            $checkResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/$waterBillId" -Method GET -Headers $headers
            Write-Host "✗ Bill still exists after deletion" -ForegroundColor Red
        } catch {
            if ($_.Exception.Response.StatusCode -eq 404) {
                Write-Host "✓ Bill correctly deleted (404 on get)" -ForegroundColor Green
            }
        }
    } catch {
        Write-Host "✗ Delete failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
