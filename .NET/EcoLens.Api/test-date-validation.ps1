# Test Date Validation Logic
Write-Host "`n=== Testing Date Validation Logic ===" -ForegroundColor Green

# Simulate the validation logic
function Test-YearValidation {
    param($yearStr, $yearInt)
    
    Write-Host "`nTesting Year: $yearStr (int: $yearInt)" -ForegroundColor Cyan
    
    # Check 1: Year string format validation
    if ($yearStr.Length -eq 4 -and !$yearStr.StartsWith("20")) {
        Write-Host "  ❌ REJECTED: Invalid 4-digit year (must start with 20)" -ForegroundColor Red
        return $false
    }
    
    if ($yearStr.Length -gt 4) {
        Write-Host "  ❌ REJECTED: Year too long" -ForegroundColor Red
        return $false
    }
    
    Write-Host "  ✓ Passed string format validation" -ForegroundColor Green
    
    # Check 2: Year range validation
    if ($yearInt -lt 2000 -or $yearInt -gt 2100) {
        Write-Host "  ❌ REJECTED: Year out of range (2000-2100)" -ForegroundColor Red
        return $false
    }
    
    Write-Host "  ✓ Passed range validation" -ForegroundColor Green
    return $true
}

# Test error dates
$testCases = @(
    @{ YearStr = "0517"; YearInt = 517 },
    @{ YearStr = "5241"; YearInt = 5241 },
    @{ YearStr = "2025"; YearInt = 2025 },
    @{ YearStr = "25"; YearInt = 25 },
    @{ YearStr = "1999"; YearInt = 1999 }
)

foreach ($test in $testCases) {
    $result = Test-YearValidation -yearStr $test.YearStr -yearInt $test.YearInt
    if ($result) {
        Write-Host "  ⚠️  WARNING: This year would be ACCEPTED!" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Testing DateTime Creation ===" -ForegroundColor Green
# Test if DateTime.TryParseExact or new DateTime can create invalid dates
try {
    $date1 = [DateTime]::ParseExact("0517-12-07", "yyyy-MM-dd", $null)
    Write-Host "  ⚠️  DateTime.ParseExact('0517-12-07') succeeded: $date1" -ForegroundColor Yellow
} catch {
    Write-Host "  ✓ DateTime.ParseExact('0517-12-07') failed (expected)" -ForegroundColor Green
}

try {
    $date2 = New-Object DateTime(517, 12, 7)
    Write-Host "  ⚠️  new DateTime(517, 12, 7) succeeded: $date2" -ForegroundColor Yellow
} catch {
    Write-Host "  ✓ new DateTime(517, 12, 7) failed (expected)" -ForegroundColor Green
}

try {
    $date3 = New-Object DateTime(5241, 10, 5)
    Write-Host "  ⚠️  new DateTime(5241, 10, 5) succeeded: $date3" -ForegroundColor Yellow
} catch {
    Write-Host "  ✓ new DateTime(5241, 10, 5) failed (expected)" -ForegroundColor Green
}
