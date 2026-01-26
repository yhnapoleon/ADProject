# Test Filtering and Pagination
$baseUrl = "http://localhost:5133"

# Login
Write-Host "`n=== Login ===" -ForegroundColor Cyan
$email = "test111856@example.com"
$loginBody = @{ email = $email; password = "Test123!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "✓ Login successful" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 1: Pagination
Write-Host "`n=== Test 1: Pagination ===" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?page=1&pageSize=2" -Method GET -Headers $headers
    Write-Host "✓ Pagination works!" -ForegroundColor Green
    Write-Host "  Total Count: $($response.totalCount)"
    Write-Host "  Page: $($response.page)"
    Write-Host "  Page Size: $($response.pageSize)"
    Write-Host "  Total Pages: $($response.totalPages)"
    Write-Host "  Items in this page: $($response.items.Count)"
    Write-Host "  Has Previous: $($response.hasPreviousPage)"
    Write-Host "  Has Next: $($response.hasNextPage)"
} catch {
    Write-Host "✗ Pagination failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Filter by Transport Mode
Write-Host "`n=== Test 2: Filter by Transport Mode ===" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?transportMode=0" -Method GET -Headers $headers
    Write-Host "✓ Filter by mode works!" -ForegroundColor Green
    Write-Host "  Total Count (Walking only): $($response.totalCount)"
    Write-Host "  All items are Walking: $(($response.items | Where-Object { $_.transportMode -ne 0 }).Count -eq 0)"
} catch {
    Write-Host "✗ Filter failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Filter by Date Range
Write-Host "`n=== Test 3: Filter by Date Range ===" -ForegroundColor Cyan
$today = Get-Date -Format "yyyy-MM-dd"
$yesterday = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?startDate=$yesterday&endDate=$today" -Method GET -Headers $headers
    Write-Host "✓ Date filter works!" -ForegroundColor Green
    Write-Host "  Total Count (last 2 days): $($response.totalCount)"
} catch {
    Write-Host "✗ Date filter failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Statistics with Date Range
Write-Host "`n=== Test 4: Statistics with Date Range ===" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics?startDate=$yesterday&endDate=$today" -Method GET -Headers $headers
    Write-Host "✓ Statistics with date range works!" -ForegroundColor Green
    Write-Host "  Total Records: $($response.totalRecords)"
    Write-Host "  Total Distance: $($response.totalDistanceKilometers) km"
    Write-Host "  Total Carbon: $($response.totalCarbonEmission) kg CO2"
} catch {
    Write-Host "✗ Statistics failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Filtering Tests Complete ===" -ForegroundColor Cyan
