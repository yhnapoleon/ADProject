# Comprehensive Google Maps API Test Script
$baseUrl = "http://localhost:5133"
$token = ""

Write-Host "=== Google Maps API Comprehensive Test ===" -ForegroundColor Green
Write-Host ""

# Wait for server
Write-Host "Waiting for server to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# 1. Register/Login
Write-Host "1. Authentication..." -ForegroundColor Cyan
try {
    $registerBody = '{"username":"maptest","email":"maptest@example.com","password":"Test123!"}'
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json" -ErrorAction Stop
    $token = $registerResponse.token
    Write-Host "   [OK] Registered and got token" -ForegroundColor Green
} catch {
    try {
        $loginBody = '{"email":"maptest@example.com","password":"Test123!"}'
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -ErrorAction Stop
        $token = $loginResponse.token
        Write-Host "   [OK] Logged in and got token" -ForegroundColor Green
    } catch {
        Write-Host "   [FAIL] Authentication failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. Test Preview Route (Google Maps Geocoding + Directions API)
Write-Host "`n2. Testing Route Preview (Geocoding + Directions API)..." -ForegroundColor Cyan
try {
    $previewBody = '{"originAddress":"Beijing Chaoyang District","destinationAddress":"Beijing Haidian District","transportMode":3}'
    $previewResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method Post -Body $previewBody -Headers $headers -ContentType "application/json" -ErrorAction Stop
    
    if ($previewResponse.originLatitude -and $previewResponse.originLongitude -and 
        $previewResponse.destinationLatitude -and $previewResponse.destinationLongitude -and
        $previewResponse.distanceKilometers -gt 0 -and
        $previewResponse.routePolyline) {
        Write-Host "   [OK] Preview successful" -ForegroundColor Green
        Write-Host "        Distance: $($previewResponse.distanceKilometers) km" -ForegroundColor Gray
        Write-Host "        Carbon: $($previewResponse.estimatedCarbonEmission) kg CO2" -ForegroundColor Gray
        Write-Host "        Has polyline: $($previewResponse.routePolyline.Length -gt 0)" -ForegroundColor Gray
    } else {
        Write-Host "   [FAIL] Preview response missing required fields" -ForegroundColor Red
    }
} catch {
    Write-Host "   [FAIL] Preview failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "        Response: $responseBody" -ForegroundColor Yellow
    }
}

# 3. Test Cache (same address should be faster)
Write-Host "`n3. Testing Cache Functionality..." -ForegroundColor Cyan
try {
    $startTime1 = Get-Date
    $previewBody2 = '{"originAddress":"Beijing Chaoyang District","destinationAddress":"Beijing Haidian District","transportMode":3}'
    $previewResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method Post -Body $previewBody2 -Headers $headers -ContentType "application/json" -ErrorAction Stop
    $endTime1 = Get-Date
    $duration1 = ($endTime1 - $startTime1).TotalMilliseconds
    
    $startTime2 = Get-Date
    $previewResponse3 = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method Post -Body $previewBody2 -Headers $headers -ContentType "application/json" -ErrorAction Stop
    $endTime2 = Get-Date
    $duration2 = ($endTime2 - $startTime2).TotalMilliseconds
    
    Write-Host "   [OK] Cache test completed" -ForegroundColor Green
    Write-Host "        First call: $([math]::Round($duration1, 2)) ms" -ForegroundColor Gray
    Write-Host "        Second call (cached): $([math]::Round($duration2, 2)) ms" -ForegroundColor Gray
    if ($duration2 -lt $duration1) {
        Write-Host "        [OK] Cache appears to be working (second call faster)" -ForegroundColor Green
    }
} catch {
    Write-Host "   [FAIL] Cache test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Test Create Travel Log (Walking - should have 0 carbon)
Write-Host "`n4. Testing Create Travel Log (Walking - 0 carbon)..." -ForegroundColor Cyan
try {
    $createBody = '{"originAddress":"Tiananmen Square, Beijing","destinationAddress":"Forbidden City, Beijing","transportMode":0,"notes":"Walking test"}'
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method Post -Body $createBody -Headers $headers -ContentType "application/json" -ErrorAction Stop
    
    if ($createResponse.id -and $createResponse.carbonEmission -eq 0) {
        Write-Host "   [OK] Created travel log (Walking)" -ForegroundColor Green
        Write-Host "        ID: $($createResponse.id)" -ForegroundColor Gray
        Write-Host "        Carbon: $($createResponse.carbonEmission) kg CO2 (correct for walking)" -ForegroundColor Gray
        $walkingId = $createResponse.id
    } else {
        Write-Host "   [FAIL] Created but carbon emission is not 0 for walking" -ForegroundColor Red
    }
} catch {
    Write-Host "   [FAIL] Create failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. Test Create Travel Log (Subway - should have carbon)
Write-Host "`n5. Testing Create Travel Log (Subway - with carbon)..." -ForegroundColor Cyan
try {
    $createBody2 = '{"originAddress":"Beijing Chaoyang District","destinationAddress":"Beijing Haidian District","transportMode":3,"notes":"Subway test"}'
    $createResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method Post -Body $createBody2 -Headers $headers -ContentType "application/json" -ErrorAction Stop
    
    if ($createResponse2.id -and $createResponse2.carbonEmission -gt 0) {
        Write-Host "   [OK] Created travel log (Subway)" -ForegroundColor Green
        Write-Host "        ID: $($createResponse2.id)" -ForegroundColor Gray
        Write-Host "        Carbon: $($createResponse2.carbonEmission) kg CO2 (correct for subway)" -ForegroundColor Gray
        $subwayId = $createResponse2.id
    } else {
        Write-Host "   [FAIL] Created but carbon emission is not greater than 0 for subway" -ForegroundColor Red
    }
} catch {
    Write-Host "   [FAIL] Create failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 6. Test Get Travel Logs List
Write-Host "`n6. Testing Get Travel Logs List..." -ForegroundColor Cyan
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels" -Method Get -Headers $headers -ErrorAction Stop
    
    if ($listResponse.items -and $listResponse.items.Count -ge 0) {
        Write-Host "   [OK] Got travel logs list" -ForegroundColor Green
        Write-Host "        Total count: $($listResponse.totalCount)" -ForegroundColor Gray
        Write-Host "        Items returned: $($listResponse.items.Count)" -ForegroundColor Gray
    } else {
        Write-Host "   [FAIL] List response format incorrect" -ForegroundColor Red
    }
} catch {
    Write-Host "   [FAIL] Get list failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 7. Test Statistics
Write-Host "`n7. Testing Statistics API..." -ForegroundColor Cyan
try {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method Get -Headers $headers -ErrorAction Stop
    
    if ($statsResponse.totalRecords -ge 0 -and $statsResponse.totalDistanceKilometers -ge 0 -and 
        $statsResponse.totalCarbonEmission -ge 0 -and $statsResponse.byTransportMode) {
        Write-Host "   [OK] Got statistics" -ForegroundColor Green
        Write-Host "        Total records: $($statsResponse.totalRecords)" -ForegroundColor Gray
        Write-Host "        Total distance: $($statsResponse.totalDistanceKilometers) km" -ForegroundColor Gray
        Write-Host "        Total carbon: $($statsResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
    } else {
        Write-Host "   [FAIL] Statistics response format incorrect" -ForegroundColor Red
    }
} catch {
    Write-Host "   [FAIL] Get statistics failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 8. Test Filter by Transport Mode
Write-Host "`n8. Testing Filter by Transport Mode..." -ForegroundColor Cyan
try {
    $filterResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?transportMode=0&page=1&pageSize=10" -Method Get -Headers $headers -ErrorAction Stop
    
    if ($filterResponse.items) {
        Write-Host "   [OK] Filter by transport mode works" -ForegroundColor Green
        Write-Host "        Filtered count: $($filterResponse.items.Count)" -ForegroundColor Gray
        $allWalking = $true
        foreach ($item in $filterResponse.items) {
            if ($item.transportMode -ne 0) {
                $allWalking = $false
                break
            }
        }
        if ($allWalking) {
            Write-Host "        [OK] All results are walking mode" -ForegroundColor Green
        } else {
            Write-Host "        [FAIL] Some results are not walking mode" -ForegroundColor Red
        }
    } else {
        Write-Host "   [FAIL] Filter response format incorrect" -ForegroundColor Red
    }
} catch {
    Write-Host "   [FAIL] Filter failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 9. Test Error Handling - Invalid Address
Write-Host "`n9. Testing Error Handling (Invalid Address)..." -ForegroundColor Cyan
try {
    $errorBody = '{"originAddress":"InvalidAddress123456789","destinationAddress":"AnotherInvalidAddress","transportMode":3}'
    Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method Post -Body $errorBody -Headers $headers -ContentType "application/json" -ErrorAction Stop
    Write-Host "   [FAIL] Should have returned 400 error for invalid address" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "   [OK] Correctly returned 400 error for invalid address" -ForegroundColor Green
    } else {
        Write-Host "   [FAIL] Wrong status code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

# 10. Test Error Handling - Missing Required Field
Write-Host "`n10. Testing Error Handling (Missing Required Field)..." -ForegroundColor Cyan
try {
    $errorBody2 = '{"originAddress":"Beijing"}'
    Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method Post -Body $errorBody2 -Headers $headers -ContentType "application/json" -ErrorAction Stop
    Write-Host "   [FAIL] Should have returned 400 error for missing field" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "   [OK] Correctly returned 400 error for missing required field" -ForegroundColor Green
    } else {
        Write-Host "   [FAIL] Wrong status code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

# 11. Test Different Transport Modes Carbon Calculation
Write-Host "`n11. Testing Different Transport Modes Carbon Calculation..." -ForegroundColor Cyan
$modes = @(
    @{mode=0; name="Walking"; expectedCarbon=0},
    @{mode=1; name="Bicycle"; expectedCarbon=0},
    @{mode=3; name="Subway"; expectedCarbon=">0"},
    @{mode=4; name="Bus"; expectedCarbon=">0"}
)

foreach ($testMode in $modes) {
    try {
        $testBody = "{\"originAddress\":\"Beijing Chaoyang District\",\"destinationAddress\":\"Beijing Haidian District\",\"transportMode\":$($testMode.mode)}"
        $testResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method Post -Body $testBody -Headers $headers -ContentType "application/json" -ErrorAction Stop
        
        $isCorrect = $false
        if ($testMode.expectedCarbon -eq 0) {
            $isCorrect = $testResponse.estimatedCarbonEmission -eq 0
        } else {
            $isCorrect = $testResponse.estimatedCarbonEmission -gt 0
        }
        
        if ($isCorrect) {
            Write-Host "   [OK] $($testMode.name): Carbon = $($testResponse.estimatedCarbonEmission) kg CO2 (correct)" -ForegroundColor Green
        } else {
            Write-Host "   [FAIL] $($testMode.name): Carbon = $($testResponse.estimatedCarbonEmission) kg CO2 (incorrect)" -ForegroundColor Red
        }
    } catch {
        Write-Host "   [FAIL] $($testMode.name): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "Please review the results above to ensure all Google Maps API functionality is working correctly." -ForegroundColor Yellow
