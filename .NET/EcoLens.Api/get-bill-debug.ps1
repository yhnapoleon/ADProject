# Get bill debug info including OCR text
param(
    [int]$BillId = 0,
    [string]$BaseUrl = "http://localhost:5133",
    [string]$Email = "",
    [string]$Token = ""
)

Write-Host "`n=== Get Bill Debug Info ===" -ForegroundColor Green

# Login (if not provided)
if ([string]::IsNullOrEmpty($Token)) {
    if ([string]::IsNullOrEmpty($Email)) {
        $timestamp = Get-Date -Format 'HHmmss'
        $email = "test$timestamp@example.com"
    } else {
        $email = $Email
    }
    
    try {
        $registerBody = @{ username = "testuser$timestamp"; email = $email; password = "Test123!" } | ConvertTo-Json
        $null = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    } catch { }
    
    $loginBody = @{ email = $email; password = "Test123!" } | ConvertTo-Json
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
} else {
    $token = $Token
}

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

# Get latest bill ID
Write-Host "`nGetting latest bill list..." -ForegroundColor Cyan
$billsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/my-bills" -Method GET -Headers $headers

if ($billsResponse.items -and $billsResponse.items.Count -gt 0) {
    $latestBill = $billsResponse.items[0]
    if ($BillId -eq 0) {
        $billId = $latestBill.id
    } else {
        $billId = $BillId
    }
    
    Write-Host "Latest Bill ID: $billId" -ForegroundColor Yellow
    Write-Host "Period from list: $($latestBill.billPeriodStart) to $($latestBill.billPeriodEnd)" -ForegroundColor White
    
    # Get debug info
    Write-Host "`nGetting debug info..." -ForegroundColor Cyan
    try {
        $debugInfo = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/$billId/debug" -Method GET -Headers $headers
        
        Write-Host "`n=== Debug Info ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($debugInfo.billId)" -ForegroundColor White
        Write-Host "Period: $($debugInfo.billPeriodStart) to $($debugInfo.billPeriodEnd)" -ForegroundColor White
        Write-Host "Start Year: $($debugInfo.startYear)" -ForegroundColor $(if ($debugInfo.startYear -ge 2000 -and $debugInfo.startYear -le 2100) { "Green" } else { "Red" })
        Write-Host "End Year: $($debugInfo.endYear)" -ForegroundColor $(if ($debugInfo.endYear -ge 2000 -and $debugInfo.endYear -le 2100) { "Green" } else { "Red" })
        Write-Host "OCR Text Length: $($debugInfo.ocrTextLength)" -ForegroundColor White
        Write-Host "OCR Confidence: $($debugInfo.ocrConfidence)" -ForegroundColor White
        
        if ($debugInfo.ocrRawText) {
            Write-Host "`n=== OCR Raw Text (First 3000 chars) ===" -ForegroundColor Yellow
            $text = $debugInfo.ocrRawText
            if ($text.Length -gt 3000) {
                Write-Host $text.Substring(0, 3000) -ForegroundColor Gray
                Write-Host "`n... (truncated, total length: $($text.Length))" -ForegroundColor DarkGray
            } else {
                Write-Host $text -ForegroundColor Gray
            }
            
            # Search for date patterns
            Write-Host "`n=== Searching for Date Patterns ===" -ForegroundColor Yellow
            $patterns = @(
                @{ Name = "Billing Period"; Pattern = "(?i)billing\s+period" },
                @{ Name = "05 Nov 2025"; Pattern = "05\s+Nov\s+2025" },
                @{ Name = "05 Dec 2025"; Pattern = "05\s+Dec\s+2025" },
                @{ Name = "Nov 2025"; Pattern = "Nov\s+2025" },
                @{ Name = "Dec 2025"; Pattern = "Dec\s+2025" },
                @{ Name = "0517-12-07"; Pattern = "0517-12-07" },
                @{ Name = "5241-10-05"; Pattern = "5241-10-05" },
                @{ Name = "YYYY-MM-DD"; Pattern = "\d{4}-\d{2}-\d{2}" },
                @{ Name = "DD MMM YYYY"; Pattern = "\d{1,2}\s+(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\w*\s+\d{4}" }
            )
            
            foreach ($p in $patterns) {
                $matches = [regex]::Matches($text, $p.Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                if ($matches.Count -gt 0) {
                    Write-Host "`nFound $($matches.Count) match(es) for: $($p.Name)" -ForegroundColor Green
                    foreach ($match in $matches) {
                        $index = $match.Index
                        $start = [Math]::Max(0, $index - 100)
                        $end = [Math]::Min($text.Length, $index + $match.Length + 100)
                        Write-Host "  Match: '$($match.Value)'" -ForegroundColor Cyan
                        Write-Host "  Context: $($text.Substring($start, $end - $start))" -ForegroundColor DarkGray
                    }
                }
            }
        } else {
            Write-Host "`nNo OCR text in debug response" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Error getting debug info: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response: $responseBody" -ForegroundColor Red
        }
    }
} else {
    Write-Host "No bills found. Please upload a bill first." -ForegroundColor Yellow
}
