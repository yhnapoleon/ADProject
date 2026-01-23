# Analyze Date Parsing Issues
# This script will help us understand what's happening with date parsing

Write-Host "`n=== Date Parsing Analysis ===" -ForegroundColor Green
Write-Host "Analyzing potential date parsing issues..." -ForegroundColor Cyan

# Test patterns that might cause issues
$testPatterns = @(
    @{
        Name = "YYYY-MM-DD with invalid year"
        Text = "0517-12-07"
        Pattern = "\b(20\d{2})[/\-\.](\d{1,2})[/\-\.](\d{1,2})\b"
    },
    @{
        Name = "DD-MM-YYYY with invalid year"
        Text = "12-07-0517"
        Pattern = "\b(\d{1,2})[/\-\.](\d{1,2})[/\-\.](20\d{2})\b"
    },
    @{
        Name = "Month name format"
        Text = "05 Nov 2025"
        Pattern = "\b(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\b"
    },
    @{
        Name = "Billing period format"
        Text = "Billing Period: 05 Nov 2025 - 05 Dec 2025"
        Pattern = "(?:billing\s+period|period)\s*:?\s*(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\s*[-–—]\s*(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\b"
    }
)

foreach ($test in $testPatterns) {
    Write-Host "`n--- Testing: $($test.Name) ---" -ForegroundColor Yellow
    Write-Host "Text: $($test.Text)" -ForegroundColor White
    Write-Host "Pattern: $($test.Pattern)" -ForegroundColor Gray
    
    $matches = [regex]::Matches($test.Text, $test.Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matches.Success) {
        Write-Host "MATCHED!" -ForegroundColor Green
        foreach ($match in $matches) {
            Write-Host "  Full match: $($match.Value)" -ForegroundColor Cyan
            for ($i = 1; $i -lt $match.Groups.Count; $i++) {
                Write-Host "    Group $i : $($match.Groups[$i].Value)" -ForegroundColor DarkCyan
            }
        }
    } else {
        Write-Host "NOT MATCHED" -ForegroundColor Red
    }
}

Write-Host "`n=== Analyzing Error Dates ===" -ForegroundColor Green
$errorDates = @("0517-12-07", "5241-10-05")

foreach ($errorDate in $errorDates) {
    Write-Host "`nError Date: $errorDate" -ForegroundColor Red
    
    # Try to understand how this could be parsed
    if ($errorDate -match "^(\d{4})-(\d{2})-(\d{2})$") {
        $year = $matches[1]
        $month = $matches[2]
        $day = $matches[3]
        
        Write-Host "  Parsed as YYYY-MM-DD:" -ForegroundColor Yellow
        Write-Host "    Year: $year (Length: $($year.Length), Starts with 20: $($year.StartsWith('20')))" -ForegroundColor White
        Write-Host "    Month: $month" -ForegroundColor White
        Write-Host "    Day: $day" -ForegroundColor White
        
        # Check if this would pass validation
        if ($year.Length -eq 4 -and !$year.StartsWith("20")) {
            Write-Host "  ❌ Would be REJECTED by year format validation" -ForegroundColor Red
        } else {
            Write-Host "  ⚠️  Would PASS year format validation (unexpected!)" -ForegroundColor Yellow
        }
        
        $yearInt = [int]$year
        if ($yearInt -lt 2000 -or $yearInt -gt 2100) {
            Write-Host "  ❌ Would be REJECTED by year range validation" -ForegroundColor Red
        } else {
            Write-Host "  ⚠️  Would PASS year range validation (unexpected!)" -ForegroundColor Yellow
        }
    }
}

Write-Host "`n=== Possible OCR Text Patterns ===" -ForegroundColor Green
Write-Host "If OCR text contains patterns like:" -ForegroundColor Cyan
Write-Host "  '05 17 12 07' - might be parsed as dates" -ForegroundColor White
Write-Host "  '52 41 10 05' - might be parsed as dates" -ForegroundColor White
Write-Host "  '0517-12-07' - direct match (should be rejected)" -ForegroundColor White
Write-Host "  '5241-10-05' - direct match (should be rejected)" -ForegroundColor White
