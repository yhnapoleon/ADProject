# Get Latest Bill Details with OCR Text
param(
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n=== Get Latest Bill Details ===" -ForegroundColor Green

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

# Get the latest bill list
Write-Host "`nGetting latest bill list..." -ForegroundColor Cyan
$billsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/my-bills" -Method GET -Headers $headers

if ($billsResponse.items -and $billsResponse.items.Count -gt 0) {
    $latestBill = $billsResponse.items[0]
    $billId = $latestBill.id
    
    Write-Host "Latest Bill ID: $billId" -ForegroundColor Yellow
    
    # Get full bill details
    Write-Host "`nGetting full bill details..." -ForegroundColor Cyan
    try {
        $fullBill = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/$billId" -Method GET -Headers $headers
        
        Write-Host "`n=== Full Bill Details ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($fullBill.id)" -ForegroundColor White
        Write-Host "Period: $($fullBill.billPeriodStart) to $($fullBill.billPeriodEnd)" -ForegroundColor White
        Write-Host "Electricity: $($fullBill.electricityUsage) kWh" -ForegroundColor White
        Write-Host "Water: $($fullBill.waterUsage) m³" -ForegroundColor White
        
        if ($fullBill.ocrRawText) {
            Write-Host "`n=== OCR Raw Text (First 2000 chars) ===" -ForegroundColor Yellow
            $text = $fullBill.ocrRawText
            if ($text.Length -gt 2000) {
                Write-Host $text.Substring(0, 2000) -ForegroundColor Gray
                Write-Host "`n... (truncated, total length: $($text.Length))" -ForegroundColor DarkGray
            } else {
                Write-Host $text -ForegroundColor Gray
            }
            
            # Search for date patterns
            Write-Host "`n=== Searching for Date Patterns ===" -ForegroundColor Yellow
            $patterns = @(
                @{ Name = "Billing Period"; Pattern = "billing\s+period|period" },
                @{ Name = "05 Nov 2025"; Pattern = "05\s+Nov\s+2025" },
                @{ Name = "05 Dec 2025"; Pattern = "05\s+Dec\s+2025" },
                @{ Name = "Nov 2025"; Pattern = "Nov\s+2025" },
                @{ Name = "Dec 2025"; Pattern = "Dec\s+2025" },
                @{ Name = "0517-12-07"; Pattern = "0517-12-07" },
                @{ Name = "5241-10-05"; Pattern = "5241-10-05" },
                @{ Name = "YYYY-MM-DD format"; Pattern = "\d{4}-\d{2}-\d{2}" }
            )
            
            foreach ($p in $patterns) {
                if ($text -match $p.Pattern) {
                    Write-Host "Found: $($p.Name)" -ForegroundColor Green
                    $index = $text.IndexOf($matches[0])
                    if ($index -ge 0) {
                        $start = [Math]::Max(0, $index - 100)
                        $end = [Math]::Min($text.Length, $index + $matches[0].Length + 100)
                        Write-Host "Context: $($text.Substring($start, $end - $start))" -ForegroundColor DarkGray
                    }
                }
            }
        } else {
            Write-Host "`nNo OCR text in response" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Error getting bill details: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host $_.Exception -ForegroundColor Red
    }
} else {
    Write-Host "No bills found. Please upload a bill first." -ForegroundColor Yellow
}
