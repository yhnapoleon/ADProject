# Final Test Summary
$baseUrl = "http://localhost:5133"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Travel API Test Summary Report" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Login
$email = "test111856@example.com"
$loginBody = @{ email = $email; password = "Test123!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get all travel logs
Write-Host "=== Database Records Verification ===" -ForegroundColor Yellow
try {
    $allLogs = Invoke-RestMethod -Uri "$baseUrl/api/travel/my-travels?pageSize=100" -Method GET -Headers $headers
    Write-Host "✓ Total records in database: $($allLogs.totalCount)" -ForegroundColor Green
    Write-Host "`nRecord Details:" -ForegroundColor Cyan
    foreach ($log in $allLogs.items) {
        Write-Host "  ID: $($log.id) | Mode: $($log.transportModeName) | Distance: $($log.distanceKilometers) km | Carbon: $($log.carbonEmission) kg CO2 | Created: $($log.createdAt)"
    }
} catch {
    Write-Host "✗ Failed to retrieve records: $($_.Exception.Message)" -ForegroundColor Red
}

# Get statistics
Write-Host "`n=== Statistics Summary ===" -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method GET -Headers $headers
    Write-Host "✓ Statistics retrieved successfully" -ForegroundColor Green
    Write-Host "  Total Records: $($stats.totalRecords)"
    Write-Host "  Total Distance: $($stats.totalDistanceKilometers) km"
    Write-Host "  Total Carbon Emission: $($stats.totalCarbonEmission) kg CO2"
    Write-Host "`n  By Transport Mode:" -ForegroundColor Cyan
    foreach ($mode in $stats.byTransportMode) {
        Write-Host "    - $($mode.transportModeName): $($mode.recordCount) records, $($mode.totalDistanceKilometers) km, $($mode.totalCarbonEmission) kg CO2"
    }
} catch {
    Write-Host "✗ Failed to get statistics: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Test Summary Complete" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
