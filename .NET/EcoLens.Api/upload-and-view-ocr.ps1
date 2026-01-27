# Upload bill and view OCR text
param(
    [string]$FilePath = "E:\OneDrive\Desktop\AD\Test.jpg",
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n=== Upload and View OCR Text ===" -ForegroundColor Green

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
}

# Upload file
Write-Host "`nUploading file..." -ForegroundColor Yellow
$tempFile = [System.IO.Path]::GetTempFileName() + ".json"
$curlPath = "C:\Windows\System32\curl.exe"
$curlCommand = "& `"$curlPath`" -X POST `"$BaseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@$FilePath`" -o `"$tempFile`" -s"
Invoke-Expression $curlCommand

if (Test-Path $tempFile) {
    $responseContent = Get-Content $tempFile -Raw
    $json = $responseContent | ConvertFrom-Json
    $billId = $json.id
    
    Write-Host "Uploaded bill ID: $billId" -ForegroundColor Green
    Write-Host "Period: $($json.billPeriodStart) to $($json.billPeriodEnd)" -ForegroundColor Yellow
    
    # Get full bill details
    Write-Host "`nGetting full bill details..." -ForegroundColor Cyan
    $bill = Invoke-RestMethod -Uri "$BaseUrl/api/UtilityBill/$billId" -Method GET -Headers $headers
    
    if ($bill.ocrRawText) {
        Write-Host "`n=== OCR Raw Text (Full) ===" -ForegroundColor Yellow
        Write-Host $bill.ocrRawText -ForegroundColor Gray
        
        # Search for problematic patterns
        Write-Host "`n=== Analyzing Date Patterns ===" -ForegroundColor Yellow
        $text = $bill.ocrRawText
        
        # Look for the problematic numbers
        if ($text -match "0517|5241") {
            Write-Host "Found problematic numbers: 0517 or 5241" -ForegroundColor Red
            $matches = [regex]::Matches($text, "(0517|5241)")
            foreach ($match in $matches) {
                $index = $match.Index
                $start = [Math]::Max(0, $index - 100)
                $end = [Math]::Min($text.Length, $index + $match.Length + 100)
                Write-Host "  Context: $($text.Substring($start, $end - $start))" -ForegroundColor DarkGray
            }
        }
        
        # Look for date-like patterns
        Write-Host "`nSearching for date patterns..." -ForegroundColor Cyan
        $datePatterns = @(
            "\d{4}-\d{2}-\d{2}",
            "\d{1,2}\s+\d{1,2}\s+\d{1,2}\s+\d{1,2}",
            "\d{1,2}\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+\d{4}",
            "billing\s+period",
            "period"
        )
        
        foreach ($pattern in $datePatterns) {
            $matches = [regex]::Matches($text, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($matches.Count -gt 0) {
                Write-Host "`nFound pattern: $pattern ($($matches.Count) matches)" -ForegroundColor Green
                foreach ($match in $matches | Select-Object -First 5) {
                    $index = $match.Index
                    $start = [Math]::Max(0, $index - 50)
                    $end = [Math]::Min($text.Length, $index + $match.Length + 50)
                    Write-Host "  Match: $($match.Value)" -ForegroundColor White
                    Write-Host "  Context: $($text.Substring($start, $end - $start))" -ForegroundColor DarkGray
                }
            }
        }
    } else {
        Write-Host "`nNo OCR text in response" -ForegroundColor Yellow
    }
    
    Remove-Item $tempFile -ErrorAction SilentlyContinue
} else {
    Write-Host "Upload failed" -ForegroundColor Red
}
