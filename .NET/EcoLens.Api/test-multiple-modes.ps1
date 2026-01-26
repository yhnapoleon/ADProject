# Test Multiple Transport Modes
$baseUrl = "http://localhost:5133"

# Login with existing user
Write-Host "`n=== Login ===" -ForegroundColor Cyan
$email = "test111856@example.com"
$loginBody = @{
    email = $email
    password = "Test123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "✓ Login successful" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test different transport modes
$modes = @(
    @{ name = "Walking"; mode = 0 },
    @{ name = "Bicycle"; mode = 1 },
    @{ name = "Bus"; mode = 4 },
    @{ name = "CarGasoline"; mode = 6 },
    @{ name = "CarElectric"; mode = 7 },
    @{ name = "Train"; mode = 8 }
)

Write-Host "`n=== Testing Different Transport Modes ===" -ForegroundColor Cyan
foreach ($transport in $modes) {
    Write-Host "`nTesting: $($transport.name) (Mode: $($transport.mode))" -ForegroundColor Yellow
    
    $createBody = @{
        originAddress = "Tiananmen Square, Beijing"
        destinationAddress = "Forbidden City, Beijing"
        transportMode = $transport.mode
        notes = "Test $($transport.name)"
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/travel" -Method POST -Headers $headers -Body $createBody
        Write-Host "  ✓ Created - Distance: $($response.distanceKilometers) km, Carbon: $($response.carbonEmission) kg CO2" -ForegroundColor Green
    } catch {
        Write-Host "  ✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Get statistics to see all modes
Write-Host "`n=== Final Statistics ===" -ForegroundColor Cyan
try {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/travel/statistics" -Method GET -Headers $headers
    Write-Host "Total Records: $($statsResponse.totalRecords)" -ForegroundColor Green
    Write-Host "Total Distance: $($statsResponse.totalDistanceKilometers) km"
    Write-Host "Total Carbon Emission: $($statsResponse.totalCarbonEmission) kg CO2"
    Write-Host "`nBy Transport Mode:" -ForegroundColor Cyan
    foreach ($modeStat in $statsResponse.byTransportMode) {
        Write-Host "  $($modeStat.transportModeName): $($modeStat.recordCount) records, $($modeStat.totalCarbonEmission) kg CO2"
    }
} catch {
    Write-Host "✗ Statistics failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
