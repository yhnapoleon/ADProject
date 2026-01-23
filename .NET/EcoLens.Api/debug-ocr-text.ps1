# Debug OCR Text - View the actual OCR recognized text
param(
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n=== Debug OCR Text ===" -ForegroundColor Green

# Login
$timestamp = Get-Date -Format 'HHmmss'
$email = "test$timestamp@example.com"

try {
    $registerBody = @{ username = "testuser$timestamp"; email = $email; password = "Test123!" } | ConvertTo-Json
    $null = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
} catch { }

$loginBody = @{ email = $email; password = "Test123!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get the latest bill
Write-Host "`nGetting latest bill..." -ForegroundColor Cyan
$billsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/my-bills" -Method GET -Headers $headers

if ($billsResponse.items -and $billsResponse.items.Count -gt 0) {
    $latestBill = $billsResponse.items[0]
    Write-Host "`n=== Latest Bill Info ===" -ForegroundColor Cyan
    Write-Host "Bill ID: $($latestBill.id)" -ForegroundColor White
    Write-Host "Period: $($latestBill.billPeriodStart) to $($latestBill.billPeriodEnd)" -ForegroundColor White
    
    # Get full bill details (which should include OCR text if available)
    Write-Host "`nGetting full bill details..." -ForegroundColor Cyan
    try {
        $fullBill = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/$($latestBill.id)" -Method GET -Headers $headers
        Write-Host "`n=== Full Bill Details ===" -ForegroundColor Cyan
        $fullBill | ConvertTo-Json -Depth 10 | Write-Host
    } catch {
        Write-Host "Could not get full bill details: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "No bills found. Please upload a bill first." -ForegroundColor Yellow
}
