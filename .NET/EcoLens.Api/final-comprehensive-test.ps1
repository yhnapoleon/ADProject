# Final Comprehensive API Test
# 全面测试所有API功能，发现问题只报告，不修改代码

$baseUrl = "http://localhost:5133"
$testResults = @()
$errors = @()

function Add-TestResult {
    param($testName, $status, $message)
    $testResults += [PSCustomObject]@{
        Test = $testName
        Status = $status
        Message = $message
        Time = Get-Date -Format "HH:mm:ss"
    }
    if ($status -eq "FAIL") {
        $script:errors += "$testName : $message"
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Final Comprehensive API Test" -ForegroundColor Cyan
Write-Host "   Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Check if API is running
Write-Host "=== Step 1: Checking API Status ===" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Add-TestResult "API Status" "PASS" "API is running"
        Write-Host "✓ API is running" -ForegroundColor Green
    }
} catch {
    Add-TestResult "API Status" "FAIL" "API is not running: $($_.Exception.Message)"
    Write-Host "✗ API is not running. Please start the API first." -ForegroundColor Red
    Write-Host "`nErrors found: $($errors.Count)" -ForegroundColor Red
    exit 1
}

# Step 2: Register and Login
Write-Host "`n=== Step 2: Authentication ===" -ForegroundColor Yellow
$timestamp = Get-Date -Format 'HHmmss'
$username = "testuser$timestamp"
$email = "test$timestamp@example.com"

try {
    $registerBody = @{
        username = $username
        email = $email
        password = "Test123!"
    } | ConvertTo-Json
    
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json" -ErrorAction Stop
    Add-TestResult "User Registration" "PASS" "User registered: $username"
    Write-Host "✓ User registered: $username" -ForegroundColor Green
} catch {
    Add-TestResult "User Registration" "FAIL" $_.Exception.Message
    Write-Host "✗ Registration failed: $($_.Exception.Message)" -ForegroundColor Red
}

try {
    $loginBody = @{
        email = $email
        password = "Test123!"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json" -ErrorAction Stop
    $token = $loginResponse.token
    if ($token -and $token.Length -gt 0) {
        Add-TestResult "User Login" "PASS" "Token obtained (length: $($token.Length))"
        Write-Host "✓ Login successful. Token obtained" -ForegroundColor Green
    } else {
        Add-TestResult "User Login" "FAIL" "Token is empty"
        Write-Host "✗ Login failed: Token is empty" -ForegroundColor Red
        exit 1
    }
} catch {
    Add-TestResult "User Login" "FAIL" $_.Exception.Message
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 3: Test Route Preview
Write-Host "`n=== Step 3: Route Preview API ===" -ForegroundColor Yellow
$previewBody = @{
    originAddress = "Tiananmen Square, Beijing"
    destinationAddress = "Forbidden City, Beijing"
    transportMode = 0
} | ConvertTo-Json

try {
    $previewResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $previewBody -ErrorAction Stop
    
    $checks = @{
        "Has distance" = $previewResponse.distanceKilometers -gt 0
        "Has duration" = $null -ne $previewResponse.durationText
        "Has coordinates" = ($previewResponse.originLatitude -ne 0) -and ($previewResponse.originLongitude -ne 0)
        "Has route polyline" = $null -ne $previewResponse.routePolyline -and $previewResponse.routePolyline.Length -gt 0
        "Has carbon emission" = $null -ne $previewResponse.estimatedCarbonEmission
    }
    
    $allPassed = $checks.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
    
    if ($allPassed -eq 0) {
        Add-TestResult "Route Preview" "PASS" "All fields present: Distance=$($previewResponse.distanceKilometers)km, Carbon=$($previewResponse.estimatedCarbonEmission)"
        Write-Host "✓ Preview successful" -ForegroundColor Green
        Write-Host "  Distance: $($previewResponse.distanceKilometers) km" -ForegroundColor Gray
        Write-Host "  Carbon: $($previewResponse.estimatedCarbonEmission) kg CO2" -ForegroundColor Gray
        Write-Host "  Has Polyline: Yes" -ForegroundColor Gray
    } else {
        $failed = ($checks.GetEnumerator() | Where-Object { $_.Value -eq $false }).Name -join ", "
        Add-TestResult "Route Preview" "FAIL" "Missing fields: $failed"
        Write-Host "✗ Preview incomplete: Missing $failed" -ForegroundColor Red
    }
} catch {
    Add-TestResult "Route Preview" "FAIL" $_.Exception.Message
    Write-Host "✗ Preview failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Create Travel Logs (Multiple modes)
Write-Host "`n=== Step 4: Create Travel Logs ===" -ForegroundColor Yellow
$createdLogIds = @()
$modes = @(
    @{ mode = 0; name = "Walking" },
    @{ mode = 6; name = "CarGasoline" },
    @{ mode = 7; name = "CarElectric" }
)

foreach ($transport in $modes) {
    $createBody = @{
        originAddress = "Tiananmen Square, Beijing"
        destinationAddress = "Forbidden City, Beijing"
        transportMode = $transport.mode
        notes = "Test $($transport.name)"
    } | ConvertTo-Json
    
    try {
        $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method POST -Headers $headers -Body $createBody -ErrorAction Stop
        
        $checks = @{
            "Has ID" = $createResponse.id -gt 0
            "Has distance" = $createResponse.distanceKilometers -gt 0
            "Has carbon" = $null -ne $createResponse.carbonEmission
            "Has polyline" = $null -ne $createResponse.routePolyline
            "Has coordinates" = ($createResponse.originLatitude -ne 0) -and ($createResponse.destinationLatitude -ne 0)
        }
        
        $allPassed = $checks.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
        
        if ($allPassed -eq 0) {
            $createdLogIds += $createResponse.id
            Add-TestResult "Create Travel Log ($($transport.name))" "PASS" "ID=$($createResponse.id), Distance=$($createResponse.distanceKilometers)km, Carbon=$($createResponse.carbonEmission)"
            Write-Host "✓ Created $($transport.name): ID=$($createResponse.id)" -ForegroundColor Green
        } else {
            $failed = ($checks.GetEnumerator() | Where-Object { $_.Value -eq $false }).Name -join ", "
            Add-TestResult "Create Travel Log ($($transport.name))" "FAIL" "Missing fields: $failed"
            Write-Host "✗ Create incomplete ($($transport.name)): Missing $failed" -ForegroundColor Red
        }
    } catch {
        Add-TestResult "Create Travel Log ($($transport.name))" "FAIL" $_.Exception.Message
        Write-Host "✗ Create failed ($($transport.name)): $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 5: Get Travel Logs List
Write-Host "`n=== Step 5: Get Travel Logs List ===" -ForegroundColor Yellow

# Test 5.1: Basic list
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?page=1&pageSize=10" -Method GET -Headers $headers -ErrorAction Stop
    
    $checks = @{
        "Has items" = $listResponse.items.Count -gt 0
        "Has totalCount" = $listResponse.totalCount -gt 0
        "Has pagination info" = ($null -ne $listResponse.page) -and ($null -ne $listResponse.pageSize)
    }
    
    $allPassed = $checks.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
    
    if ($allPassed -eq 0) {
        Add-TestResult "Get Travel Logs List" "PASS" "Total=$($listResponse.totalCount), Items=$($listResponse.items.Count)"
        Write-Host "✓ List retrieved: $($listResponse.totalCount) total, $($listResponse.items.Count) in page" -ForegroundColor Green
    } else {
        $failed = ($checks.GetEnumerator() | Where-Object { $_.Value -eq $false }).Name -join ", "
        Add-TestResult "Get Travel Logs List" "FAIL" "Missing: $failed"
        Write-Host "✗ List incomplete: Missing $failed" -ForegroundColor Red
    }
} catch {
    Add-TestResult "Get Travel Logs List" "FAIL" $_.Exception.Message
    Write-Host "✗ List failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5.2: Filter by transport mode
try {
    $filterResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?transportMode=0" -Method GET -Headers $headers -ErrorAction Stop
    $allWalking = ($filterResponse.items | Where-Object { $_.transportMode -ne 0 }).Count -eq 0
    
    if ($allWalking) {
        Add-TestResult "Filter by Transport Mode" "PASS" "All items are Walking mode"
        Write-Host "✓ Filter by mode works" -ForegroundColor Green
    } else {
        Add-TestResult "Filter by Transport Mode" "FAIL" "Filter not working correctly"
        Write-Host "✗ Filter by mode failed" -ForegroundColor Red
    }
} catch {
    Add-TestResult "Filter by Transport Mode" "FAIL" $_.Exception.Message
    Write-Host "✗ Filter failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5.3: Pagination
try {
    $page1Response = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?page=1&pageSize=2" -Method GET -Headers $headers -ErrorAction Stop
    
    $checks = @{
        "Page 1 has items" = $page1Response.items.Count -le 2
        "Has pagination fields" = ($null -ne $page1Response.hasPreviousPage) -and ($null -ne $page1Response.hasNextPage)
        "Total pages calculated" = $page1Response.totalPages -gt 0
    }
    
    $allPassed = $checks.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
    
    if ($allPassed -eq 0) {
        Add-TestResult "Pagination" "PASS" "Page=$($page1Response.page), Size=$($page1Response.pageSize), Total=$($page1Response.totalCount)"
        Write-Host "✓ Pagination works" -ForegroundColor Green
    } else {
        $failed = ($checks.GetEnumerator() | Where-Object { $_.Value -eq $false }).Name -join ", "
        Add-TestResult "Pagination" "FAIL" "Missing: $failed"
        Write-Host "✗ Pagination incomplete: Missing $failed" -ForegroundColor Red
    }
} catch {
    Add-TestResult "Pagination" "FAIL" $_.Exception.Message
    Write-Host "✗ Pagination failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 6: Get Single Travel Log
Write-Host "`n=== Step 6: Get Single Travel Log ===" -ForegroundColor Yellow
if ($createdLogIds.Count -gt 0) {
    $testId = $createdLogIds[0]
    try {
        $singleResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/$testId" -Method GET -Headers $headers -ErrorAction Stop
        
        $checks = @{
            "Has ID" = $singleResponse.id -eq $testId
            "Has distance" = $singleResponse.distanceKilometers -gt 0
            "Has carbon" = $null -ne $singleResponse.carbonEmission
            "Has coordinates" = ($singleResponse.originLatitude -ne 0) -and ($singleResponse.destinationLatitude -ne 0)
        }
        
        $allPassed = $checks.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
        
        if ($allPassed -eq 0) {
            Add-TestResult "Get Single Travel Log" "PASS" "ID=$testId, Distance=$($singleResponse.distanceKilometers)km"
            Write-Host "✓ Single log retrieved: ID=$testId" -ForegroundColor Green
        } else {
            $failed = ($checks.GetEnumerator() | Where-Object { $_.Value -eq $false }).Name -join ", "
            Add-TestResult "Get Single Travel Log" "FAIL" "Missing: $failed"
            Write-Host "✗ Single log incomplete: Missing $failed" -ForegroundColor Red
        }
    } catch {
        Add-TestResult "Get Single Travel Log" "FAIL" $_.Exception.Message
        Write-Host "✗ Single log failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Add-TestResult "Get Single Travel Log" "SKIP" "No logs created to test"
    Write-Host "⚠ No logs to test" -ForegroundColor Yellow
}

# Step 7: Get Statistics
Write-Host "`n=== Step 7: Get Statistics ===" -ForegroundColor Yellow
try {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method GET -Headers $headers -ErrorAction Stop
    
    $checks = @{
        "Has total records" = $statsResponse.totalRecords -gt 0
        "Has total distance" = $statsResponse.totalDistanceKilometers -gt 0
        "Has total carbon" = $null -ne $statsResponse.totalCarbonEmission
        "Has by transport mode" = $statsResponse.byTransportMode.Count -gt 0
    }
    
    $allPassed = $checks.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
    
    if ($allPassed -eq 0) {
        Add-TestResult "Get Statistics" "PASS" "Records=$($statsResponse.totalRecords), Distance=$($statsResponse.totalDistanceKilometers)km, Carbon=$($statsResponse.totalCarbonEmission)"
        Write-Host "✓ Statistics retrieved" -ForegroundColor Green
        Write-Host "  Total Records: $($statsResponse.totalRecords)" -ForegroundColor Gray
        Write-Host "  Total Distance: $($statsResponse.totalDistanceKilometers) km" -ForegroundColor Gray
        Write-Host "  Total Carbon: $($statsResponse.totalCarbonEmission) kg CO2" -ForegroundColor Gray
    } else {
        $failed = ($checks.GetEnumerator() | Where-Object { $_.Value -eq $false }).Name -join ", "
        Add-TestResult "Get Statistics" "FAIL" "Missing: $failed"
        Write-Host "✗ Statistics incomplete: Missing $failed" -ForegroundColor Red
    }
} catch {
    Add-TestResult "Get Statistics" "FAIL" $_.Exception.Message
    Write-Host "✗ Statistics failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 8: Error Scenarios
Write-Host "`n=== Step 8: Error Scenarios ===" -ForegroundColor Yellow

# Test 8.1: Invalid address
try {
    $invalidBody = @{
        originAddress = "InvalidAddress12345XYZ"
        destinationAddress = "AnotherInvalidAddress67890"
        transportMode = 0
    } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $invalidBody -ErrorAction Stop | Out-Null
    Add-TestResult "Error: Invalid Address" "FAIL" "Should return 400 but succeeded"
    Write-Host "✗ Invalid address test failed: Should return error" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 400) {
        Add-TestResult "Error: Invalid Address" "PASS" "Correctly returned 400"
        Write-Host "✓ Invalid address correctly returns 400" -ForegroundColor Green
    } else {
        Add-TestResult "Error: Invalid Address" "FAIL" "Returned $statusCode instead of 400"
        Write-Host "✗ Invalid address returned $statusCode instead of 400" -ForegroundColor Red
    }
}

# Test 8.2: Unauthorized access
try {
    $noAuthHeaders = @{ "Content-Type" = "application/json" }
    $body = @{
        originAddress = "Tiananmen Square, Beijing"
        destinationAddress = "Forbidden City, Beijing"
        transportMode = 0
    } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $noAuthHeaders -Body $body -ErrorAction Stop | Out-Null
    Add-TestResult "Error: Unauthorized" "FAIL" "Should return 401 but succeeded"
    Write-Host "✗ Unauthorized test failed: Should return 401" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Add-TestResult "Error: Unauthorized" "PASS" "Correctly returned 401"
        Write-Host "✓ Unauthorized correctly returns 401" -ForegroundColor Green
    } else {
        Add-TestResult "Error: Unauthorized" "FAIL" "Returned $statusCode instead of 401"
        Write-Host "✗ Unauthorized returned $statusCode instead of 401" -ForegroundColor Red
    }
}

# Test 8.3: Non-existent record
try {
    Invoke-RestMethod -Uri "$baseUrl/api/travel/99999" -Method GET -Headers $headers -ErrorAction Stop | Out-Null
    Add-TestResult "Error: Non-existent Record" "FAIL" "Should return 404 but succeeded"
    Write-Host "✗ Non-existent record test failed: Should return 404" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 404) {
        Add-TestResult "Error: Non-existent Record" "PASS" "Correctly returned 404"
        Write-Host "✓ Non-existent record correctly returns 404" -ForegroundColor Green
    } else {
        Add-TestResult "Error: Non-existent Record" "FAIL" "Returned $statusCode instead of 404"
        Write-Host "✗ Non-existent record returned $statusCode instead of 404" -ForegroundColor Red
    }
}

# Step 9: Delete Travel Log
Write-Host "`n=== Step 9: Delete Travel Log ===" -ForegroundColor Yellow
if ($createdLogIds.Count -gt 0) {
    $deleteId = $createdLogIds[-1]  # Delete the last one
    try {
        $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/$deleteId" -Method DELETE -Headers $headers -ErrorAction Stop
        Add-TestResult "Delete Travel Log" "PASS" "ID=$deleteId deleted"
        Write-Host "✓ Travel log deleted: ID=$deleteId" -ForegroundColor Green
    } catch {
        Add-TestResult "Delete Travel Log" "FAIL" $_.Exception.Message
        Write-Host "✗ Delete failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Add-TestResult "Delete Travel Log" "SKIP" "No logs to delete"
    Write-Host "⚠ No logs to delete" -ForegroundColor Yellow
}

# Final Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passed = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$skipped = ($testResults | Where-Object { $_.Status -eq "SKIP" }).Count
$total = $testResults.Count

Write-Host "Total Tests: $total" -ForegroundColor White
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host "Skipped: $skipped" -ForegroundColor Yellow

if ($failed -gt 0) {
    Write-Host "`n❌ Issues Found:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
} else {
    Write-Host "`n✅ All tests passed! API is working correctly." -ForegroundColor Green
}

Write-Host "`n========================================`n" -ForegroundColor Cyan

# Export results
$testResults | Format-Table -AutoSize
$testResults | Export-Csv -Path "test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').csv" -NoTypeInformation
Write-Host "Test results exported to CSV file." -ForegroundColor Gray
