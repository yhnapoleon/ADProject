# Test Error Scenarios
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

# Test 1: Invalid address (cannot geocode)
Write-Host "`n=== Test 1: Invalid Address ===" -ForegroundColor Cyan
$invalidBody = @{
    originAddress = "InvalidAddress12345XYZ"
    destinationAddress = "AnotherInvalidAddress67890"
    transportMode = 0
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $invalidBody
    Write-Host "✗ Should have failed but succeeded" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✓ Correctly returned error: $statusCode" -ForegroundColor Green
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $responseBody = $reader.ReadToEnd()
    Write-Host "  Error message: $responseBody"
}

# Test 2: Missing required fields
Write-Host "`n=== Test 2: Missing Required Fields ===" -ForegroundColor Cyan
$missingBody = @{
    originAddress = "Tiananmen Square, Beijing"
    # destinationAddress is missing
    transportMode = 0
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $missingBody
    Write-Host "✗ Should have failed but succeeded" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✓ Correctly returned error: $statusCode" -ForegroundColor Green
}

# Test 3: Unauthorized access (no token)
Write-Host "`n=== Test 3: Unauthorized Access ===" -ForegroundColor Cyan
$noAuthHeaders = @{ "Content-Type" = "application/json" }
$body = @{
    originAddress = "Tiananmen Square, Beijing"
    destinationAddress = "Forbidden City, Beijing"
    transportMode = 0
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $noAuthHeaders -Body $body
    Write-Host "✗ Should have failed but succeeded" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✓ Correctly returned error: $statusCode" -ForegroundColor Green
    if ($statusCode -eq 401) {
        Write-Host "  ✓ Correctly returned 401 Unauthorized" -ForegroundColor Green
    }
}

# Test 4: Access non-existent travel log
Write-Host "`n=== Test 4: Access Non-existent Travel Log ===" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/99999" -Method GET -Headers $headers
    Write-Host "✗ Should have failed but succeeded" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✓ Correctly returned error: $statusCode" -ForegroundColor Green
    if ($statusCode -eq 404) {
        Write-Host "  ✓ Correctly returned 404 Not Found" -ForegroundColor Green
    }
}

# Test 5: Invalid transport mode value
Write-Host "`n=== Test 5: Invalid Transport Mode ===" -ForegroundColor Cyan
$invalidModeBody = @{
    originAddress = "Tiananmen Square, Beijing"
    destinationAddress = "Forbidden City, Beijing"
    transportMode = 999  # Invalid mode
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/travel/preview" -Method POST -Headers $headers -Body $invalidModeBody
    Write-Host "✗ Should have failed but succeeded" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✓ Correctly returned error: $statusCode" -ForegroundColor Green
}

Write-Host "`n=== Error Scenario Tests Complete ===" -ForegroundColor Cyan
