# Upload bill and analyze OCR text
param(
    [string]$FilePath = "E:\OneDrive\Desktop\AD\Test.jpg",
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n=== Upload and Analyze ===" -ForegroundColor Green

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

Write-Host "Token: $($token.Substring(0,20))..." -ForegroundColor Gray

# Upload using curl
$tempFile = [System.IO.Path]::GetTempFileName() + ".json"
$curlPath = "C:\Windows\System32\curl.exe"
$curlCommand = "& `"$curlPath`" -X POST `"$BaseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@$FilePath`" -o `"$tempFile`" -s"

Write-Host "`nUploading file..." -ForegroundColor Yellow
Invoke-Expression $curlCommand

if (Test-Path $tempFile) {
    $responseContent = Get-Content $tempFile -Raw
    $json = $responseContent | ConvertFrom-Json
    
    Write-Host "`n=== Upload Response ===" -ForegroundColor Cyan
    Write-Host "Bill ID: $($json.id)" -ForegroundColor White
    Write-Host "Period: $($json.billPeriodStart) to $($json.billPeriodEnd)" -ForegroundColor White
    
    # Save email and token for next script
    $script:Email = $email
    $script:Token = $token
    
    # Get full details
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    Write-Host "`nGetting full bill details..." -ForegroundColor Cyan
    $fullBill = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/$($json.id)" -Method GET -Headers $headers
    
    if ($fullBill.ocrRawText) {
        Write-Host "`n=== OCR Raw Text (First 3000 chars) ===" -ForegroundColor Yellow
        $text = $fullBill.ocrRawText
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
        Write-Host "`nNo OCR text in response" -ForegroundColor Yellow
    }
    
    Remove-Item $tempFile -ErrorAction SilentlyContinue
} else {
    Write-Host "✗ No response file created" -ForegroundColor Red
}
