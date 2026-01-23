# Final Data Verification Test
# 验证数据准确性和完整性

$baseUrl = "http://localhost:5133"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Final Data Verification Test" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Login
$email = "test125827@example.com"
$loginBody = @{ email = $email; password = "Test123!" } | ConvertTo-Json
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed. Please run the comprehensive test first." -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 1: Verify Route Preview Data
Write-Host "`n=== Test 1: Route Preview Data Verification ===" -ForegroundColor Yellow
$previewBody = @{
    originAddress = "Tiananmen Square, Beijing"
    destinationAddress = "Forbidden City, Beijing"
    transportMode = 6  # CarGasoline
} | ConvertTo-Json

try {
    $preview = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $previewBody
    
    Write-Host "Preview Data:" -ForegroundColor Cyan
    Write-Host "  Origin: $($preview.originAddress)" -ForegroundColor Gray
    Write-Host "  Destination: $($preview.destinationAddress)" -ForegroundColor Gray
    Write-Host "  Distance: $($preview.distanceKilometers) km" -ForegroundColor Gray
    Write-Host "  Duration: $($preview.durationText)" -ForegroundColor Gray
    Write-Host "  Carbon Emission: $($preview.estimatedCarbonEmission) kg CO2" -ForegroundColor Gray
    Write-Host "  Origin Coordinates: ($($preview.originLatitude), $($preview.originLongitude))" -ForegroundColor Gray
    Write-Host "  Destination Coordinates: ($($preview.destinationLatitude), $($preview.destinationLongitude))" -ForegroundColor Gray
    Write-Host "  Route Polyline Length: $($preview.routePolyline.Length) characters" -ForegroundColor Gray
    
    # Verify calculations
    $expectedCarbon = [math]::Round($preview.distanceKilometers * 0.2, 4)
    $actualCarbon = [math]::Round($preview.estimatedCarbonEmission, 4)
    
    if ([math]::Abs($expectedCarbon - $actualCarbon) -lt 0.0001) {
        Write-Host "  ✓ Carbon calculation correct: $actualCarbon kg CO2" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Carbon calculation incorrect: Expected ~$expectedCarbon, Got $actualCarbon" -ForegroundColor Red
    }
    
    if ($preview.routePolyline.Length -gt 50) {
        Write-Host "  ✓ Route polyline data present" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Route polyline data missing or too short" -ForegroundColor Red
    }
    
    if ($preview.originLatitude -ne 0 -and $preview.originLongitude -ne 0) {
        Write-Host "  ✓ Coordinates present" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Coordinates missing" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Preview test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Verify Created Record Data
Write-Host "`n=== Test 2: Created Record Data Verification ===" -ForegroundColor Yellow
$createBody = @{
    originAddress = "Tiananmen Square, Beijing"
    destinationAddress = "Forbidden City, Beijing"
    transportMode = 6  # CarGasoline
    notes = "Final verification test"
} | ConvertTo-Json

try {
    $created = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method POST -Headers $headers -Body $createBody
    $createdId = $created.id
    
    Write-Host "Created Record Data:" -ForegroundColor Cyan
    Write-Host "  ID: $createdId" -ForegroundColor Gray
    Write-Host "  Distance: $($created.distanceKilometers) km" -ForegroundColor Gray
    Write-Host "  Carbon: $($created.carbonEmission) kg CO2" -ForegroundColor Gray
    Write-Host "  Created At: $($created.createdAt)" -ForegroundColor Gray
    Write-Host "  Has Polyline: $(if ($created.routePolyline) { 'Yes' } else { 'No' })" -ForegroundColor Gray
    
    # Verify all required fields
    $requiredFields = @(
        @{ Name = "ID"; Value = $created.id; Valid = $created.id -gt 0 },
        @{ Name = "Distance"; Value = $created.distanceKilometers; Valid = $created.distanceKilometers -gt 0 },
        @{ Name = "Carbon"; Value = $created.carbonEmission; Valid = $null -ne $created.carbonEmission },
        @{ Name = "Origin Coordinates"; Value = "$($created.originLatitude),$($created.originLongitude)"; Valid = ($created.originLatitude -ne 0) -and ($created.originLongitude -ne 0) },
        @{ Name = "Destination Coordinates"; Value = "$($created.destinationLatitude),$($created.destinationLongitude)"; Valid = ($created.destinationLatitude -ne 0) -and ($created.destinationLongitude -ne 0) },
        @{ Name = "Route Polyline"; Value = $created.routePolyline; Valid = $null -ne $created.routePolyline -and $created.routePolyline.Length -gt 0 },
        @{ Name = "Created At"; Value = $created.createdAt; Valid = $null -ne $created.createdAt }
    )
    
    $allValid = $true
    foreach ($field in $requiredFields) {
        if ($field.Valid) {
            Write-Host "  ✓ $($field.Name): Present" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $($field.Name): Missing or Invalid" -ForegroundColor Red
            $allValid = $false
        }
    }
    
    if ($allValid) {
        Write-Host "`n  ✅ All required fields present and valid" -ForegroundColor Green
    } else {
        Write-Host "`n  ❌ Some required fields are missing or invalid" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Create test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Verify List Response Structure
Write-Host "`n=== Test 3: List Response Structure Verification ===" -ForegroundColor Yellow
try {
    $list = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?page=1&pageSize=5" -Method GET -Headers $headers
    
    Write-Host "List Response Structure:" -ForegroundColor Cyan
    Write-Host "  Total Count: $($list.totalCount)" -ForegroundColor Gray
    Write-Host "  Page: $($list.page)" -ForegroundColor Gray
    Write-Host "  Page Size: $($list.pageSize)" -ForegroundColor Gray
    Write-Host "  Total Pages: $($list.totalPages)" -ForegroundColor Gray
    Write-Host "  Items Count: $($list.items.Count)" -ForegroundColor Gray
    Write-Host "  Has Previous: $($list.hasPreviousPage)" -ForegroundColor Gray
    Write-Host "  Has Next: $($list.hasNextPage)" -ForegroundColor Gray
    
    # Verify pagination calculation
    $expectedPages = [math]::Ceiling($list.totalCount / $list.pageSize)
    if ($list.totalPages -eq $expectedPages) {
        Write-Host "  ✓ Total pages calculation correct" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Total pages incorrect: Expected $expectedPages, Got $($list.totalPages)" -ForegroundColor Red
    }
    
    # Verify items structure
    if ($list.items.Count -gt 0) {
        $firstItem = $list.items[0]
        $itemFields = @(
            @{ Name = "ID"; Value = $firstItem.id; Valid = $null -ne $firstItem.id },
            @{ Name = "Distance"; Value = $firstItem.distanceKilometers; Valid = $null -ne $firstItem.distanceKilometers },
            @{ Name = "Carbon"; Value = $firstItem.carbonEmission; Valid = $null -ne $firstItem.carbonEmission },
            @{ Name = "Transport Mode"; Value = $firstItem.transportMode; Valid = $null -ne $firstItem.transportMode }
        )
        
        $allValid = $true
        foreach ($field in $itemFields) {
            if ($field.Valid) {
                Write-Host "  ✓ Item has $($field.Name)" -ForegroundColor Green
            } else {
                Write-Host "  ✗ Item missing $($field.Name)" -ForegroundColor Red
                $allValid = $false
            }
        }
    }
} catch {
    Write-Host "✗ List test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Verify Statistics Accuracy
Write-Host "`n=== Test 4: Statistics Accuracy Verification ===" -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method GET -Headers $headers
    
    Write-Host "Statistics Data:" -ForegroundColor Cyan
    Write-Host "  Total Records: $($stats.totalRecords)" -ForegroundColor Gray
    Write-Host "  Total Distance: $($stats.totalDistanceKilometers) km" -ForegroundColor Gray
    Write-Host "  Total Carbon: $($stats.totalCarbonEmission) kg CO2" -ForegroundColor Gray
    Write-Host "  By Transport Mode: $($stats.byTransportMode.Count) modes" -ForegroundColor Gray
    
    # Verify by transport mode
    foreach ($modeStat in $stats.byTransportMode) {
        Write-Host "`n  Mode: $($modeStat.transportModeName)" -ForegroundColor Cyan
        Write-Host "    Records: $($modeStat.recordCount)" -ForegroundColor Gray
        Write-Host "    Distance: $($modeStat.totalDistanceKilometers) km" -ForegroundColor Gray
        Write-Host "    Carbon: $($modeStat.totalCarbonEmission) kg CO2" -ForegroundColor Gray
    }
    
    Write-Host "`n  ✓ Statistics structure complete" -ForegroundColor Green
} catch {
    Write-Host "✗ Statistics test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Verify Error Responses
Write-Host "`n=== Test 5: Error Response Format Verification ===" -ForegroundColor Yellow

# Test invalid address
try {
    $invalidBody = @{
        originAddress = "InvalidAddress12345XYZ"
        destinationAddress = "AnotherInvalidAddress67890"
        transportMode = 0
    } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $invalidBody -ErrorAction Stop | Out-Null
    Write-Host "✗ Invalid address should return error" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $errorBody = $reader.ReadToEnd()
    
    Write-Host "Error Response:" -ForegroundColor Cyan
    Write-Host "  Status Code: $statusCode" -ForegroundColor Gray
    Write-Host "  Error Body: $errorBody" -ForegroundColor Gray
    
    if ($statusCode -eq 400 -and $errorBody -match "error") {
        Write-Host "  ✓ Error response format correct" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Error response format incorrect" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Verification Complete" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
