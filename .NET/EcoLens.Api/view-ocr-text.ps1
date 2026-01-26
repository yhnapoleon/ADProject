# View OCR Text from Latest Bill
param(
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n=== View OCR Text from Latest Bill ===" -ForegroundColor Green

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
    
    if ($latestBill.ocrRawText) {
        Write-Host "`n=== OCR Raw Text (First 2000 chars) ===" -ForegroundColor Yellow
        $text = $latestBill.ocrRawText
        if ($text.Length -gt 2000) {
            Write-Host $text.Substring(0, 2000) -ForegroundColor Gray
            Write-Host "`n... (truncated, total length: $($text.Length))" -ForegroundColor DarkGray
        } else {
            Write-Host $text -ForegroundColor Gray
        }
        
        # Search for date patterns
        Write-Host "`n=== Searching for Date Patterns ===" -ForegroundColor Yellow
        $datePatterns = @(
            "05 Nov 2025",
            "05 Dec 2025",
            "Nov 2025",
            "Dec 2025",
            "2025",
            "Billing Period",
            "Period"
        )
        
        foreach ($pattern in $datePatterns) {
            if ($text -match $pattern) {
                Write-Host "Found: $pattern" -ForegroundColor Green
                # Show context
                $index = $text.IndexOf($pattern)
                if ($index -ge 0) {
                    $start = [Math]::Max(0, $index - 50)
                    $end = [Math]::Min($text.Length, $index + $pattern.Length + 50)
                    Write-Host "Context: $($text.Substring($start, $end - $start))" -ForegroundColor DarkGray
                }
            }
        }
    } else {
        Write-Host "`nNo OCR text found in response." -ForegroundColor Yellow
    }
} else {
    Write-Host "No bills found. Please upload a bill first." -ForegroundColor Yellow
}
