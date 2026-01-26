# Travel API Test Script
$baseUrl = "http://localhost:5133"

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

# Step 2: Test Route Preview
Write-Host "`n=== Step 2: Test Route Preview API ===" -ForegroundColor Cyan
$previewBody = @{
    originAddress = "Tiananmen Square, Beijing"
    destinationAddress = "Forbidden City, Beijing"
    transportMode = 0
} | ConvertTo-Json

try {
    $previewResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $previewBody
    Write-Host "✓ Preview successful!" -ForegroundColor Green
    Write-Host "  Distance: $($previewResponse.distanceKilometers) km"
    Write-Host "  Duration: $($previewResponse.durationText)"
    Write-Host "  Carbon Emission: $($previewResponse.carbonEmission) kg CO2"
    Write-Host "  Has Route Polyline: $(if ($previewResponse.routePolyline) { "Yes (length: $($previewResponse.routePolyline.Length))" } else { "No" })"
} catch {
    Write-Host "✗ Preview failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
}

# Step 3: Create Travel Log
Write-Host "`n=== Step 3: Create Travel Log ===" -ForegroundColor Cyan
$createBody = @{
    originAddress = "Tiananmen Square, Beijing"
    destinationAddress = "Forbidden City, Beijing"
    transportMode = 0
    notes = "Test travel log"
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method POST -Headers $headers -Body $createBody
    $travelLogId = $createResponse.id
    Write-Host "✓ Travel log created! ID: $travelLogId" -ForegroundColor Green
    Write-Host "  Distance: $($createResponse.distanceKilometers) km"
    Write-Host "  Duration: $($createResponse.durationText)"
    Write-Host "  Carbon Emission: $($createResponse.carbonEmission) kg CO2"
    Write-Host "  Has Route Polyline: $(if ($createResponse.routePolyline) { "Yes" } else { "No" })"
} catch {
    Write-Host "✗ Create failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
    $travelLogId = $null
}

# Step 4: Get Travel Logs List
Write-Host "`n=== Step 4: Get Travel Logs List ===" -ForegroundColor Cyan
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?page=1&pageSize=10" -Method GET -Headers $headers
    Write-Host "✓ List retrieved!" -ForegroundColor Green
    Write-Host "  Total Count: $($listResponse.totalCount)"
    Write-Host "  Page: $($listResponse.page)"
    Write-Host "  Page Size: $($listResponse.pageSize)"
    Write-Host "  Total Pages: $($listResponse.totalPages)"
    Write-Host "  Items Count: $($listResponse.items.Count)"
} catch {
    Write-Host "✗ List failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Get Statistics
Write-Host "`n=== Step 5: Get Statistics ===" -ForegroundColor Cyan
try {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method GET -Headers $headers
    Write-Host "✓ Statistics retrieved!" -ForegroundColor Green
    Write-Host "  Total Records: $($statsResponse.totalRecords)"
    Write-Host "  Total Distance: $($statsResponse.totalDistanceKilometers) km"
    Write-Host "  Total Carbon Emission: $($statsResponse.totalCarbonEmission) kg CO2"
    Write-Host "  By Transport Mode: $($statsResponse.byTransportMode.Count) modes"
} catch {
    Write-Host "✗ Statistics failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 6: Get Single Travel Log (if created)
if ($travelLogId) {
    Write-Host "`n=== Step 6: Get Single Travel Log ===" -ForegroundColor Cyan
    try {
        $singleResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/$travelLogId" -Method GET -Headers $headers
        Write-Host "✓ Single log retrieved! ID: $($singleResponse.id)" -ForegroundColor Green
        Write-Host "  Origin: $($singleResponse.originAddress)"
        Write-Host "  Destination: $($singleResponse.destinationAddress)"
    } catch {
        Write-Host "✗ Get single failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
