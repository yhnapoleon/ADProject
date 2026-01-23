# Get OCR text from latest bill
param(
    [string]$BaseUrl = "http://localhost:5133",
    [int]$BillId = 0
)

Write-Host "`n=== Get Bill OCR Text ===" -ForegroundColor Green

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

# Get bill ID if not provided
if ($BillId -eq 0) {
    Write-Host "Getting latest bill ID..." -ForegroundColor Cyan
    $billsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/my-bills" -Method GET -Headers $headers
    if ($billsResponse.items -and $billsResponse.items.Count -gt 0) {
        $BillId = $billsResponse.items[0].id
        Write-Host "Found latest bill ID: $BillId" -ForegroundColor Green
    } else {
        Write-Host "No bills found" -ForegroundColor Red
        exit
    }
}

# Get bill details
Write-Host "`nGetting bill details for ID: $BillId..." -ForegroundColor Cyan
$bill = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/$BillId" -Method GET -Headers $headers

Write-Host "`n=== Bill Info ===" -ForegroundColor Cyan
Write-Host "ID: $($bill.id)" -ForegroundColor White
Write-Host "Period: $($bill.billPeriodStart) to $($bill.billPeriodEnd)" -ForegroundColor White

if ($bill.ocrRawText) {
    Write-Host "`n=== OCR Raw Text (Full) ===" -ForegroundColor Yellow
    Write-Host $bill.ocrRawText -ForegroundColor Gray
    
    # Search for date patterns
    Write-Host "`n=== Searching for Date Patterns ===" -ForegroundColor Yellow
    $text = $bill.ocrRawText
    
    # Look for patterns that might cause issues
    $patterns = @(
        "05 Nov 2025",
        "05 Dec 2025",
        "Nov 2025",
        "Dec 2025",
        "2025",
        "Billing Period",
        "Period",
        "0517",
        "5241",
        "\d{4}-\d{2}-\d{2}",
        "\d{1,2}\s+\d{1,2}\s+\d{1,2}\s+\d{1,2}"
    )
    
    foreach ($pattern in $patterns) {
        if ($text -match $pattern) {
            Write-Host "Found pattern: $pattern" -ForegroundColor Green
            $matches = [regex]::Matches($text, $pattern)
            foreach ($match in $matches) {
                $index = $match.Index
                $start = [Math]::Max(0, $index - 50)
                $end = [Math]::Min($text.Length, $index + $match.Length + 50)
                Write-Host "  Context: $($text.Substring($start, $end - $start))" -ForegroundColor DarkGray
            }
        }
    }
} else {
    Write-Host "`nNo OCR text found" -ForegroundColor Yellow
}
