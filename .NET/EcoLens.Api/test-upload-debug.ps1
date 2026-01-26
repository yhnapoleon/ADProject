# Debug upload test - show raw response
param(
    [string]$FilePath = "E:\OneDrive\Desktop\AD\Test.jpg",
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n=== Debug Upload Test ===" -ForegroundColor Green

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

# Upload using curl and save to file
$tempFile = [System.IO.Path]::GetTempFileName() + ".json"
$curlPath = "C:\Windows\System32\curl.exe"
$curlCommand = "& `"$curlPath`" -X POST `"$BaseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@$FilePath`" -o `"$tempFile`" -s"

Write-Host "`nUploading file..." -ForegroundColor Yellow
Invoke-Expression $curlCommand

if (Test-Path $tempFile) {
    $responseContent = Get-Content $tempFile -Raw
    Write-Host "`n=== Raw Response ===" -ForegroundColor Cyan
    Write-Host $responseContent -ForegroundColor White
    
    try {
        $json = $responseContent | ConvertFrom-Json
        
        Write-Host "`n=== Parsed Response ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($json.id)" -ForegroundColor White
        Write-Host "Bill Type: $($json.billTypeName)" -ForegroundColor White
        Write-Host "Period: $($json.billPeriodStart) to $($json.billPeriodEnd)" -ForegroundColor White
        Write-Host "Input Method: $($json.inputMethodName)" -ForegroundColor White
        
        if ($json.ocrConfidence) {
            Write-Host "OCR Confidence: $([math]::Round($json.ocrConfidence * 100, 2))%" -ForegroundColor White
        }
        
        Write-Host "`n=== Usage ===" -ForegroundColor Cyan
        if ($json.electricityUsage) { Write-Host "Electricity: $($json.electricityUsage) kWh" -ForegroundColor White }
        if ($json.waterUsage) { Write-Host "Water: $($json.waterUsage) m³" -ForegroundColor White }
        if ($json.gasUsage) { Write-Host "Gas: $($json.gasUsage)" -ForegroundColor White }
        
        Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
        Write-Host "Electricity: $($json.electricityCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Water: $($json.waterCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Gas: $($json.gasCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Total: $($json.totalCarbonEmission) kg CO2" -ForegroundColor Green
        
        # Verify against expected values
        Write-Host "`n=== OCR Raw Text (First 1000 chars) ===" -ForegroundColor Yellow
if ($json.ocrRawText) {
    $text = $response.ocrRawText
    if ($text.Length -gt 1000) {
        Write-Host $text.Substring(0, 1000) -ForegroundColor Gray
        Write-Host "`n... (truncated, total length: $($text.Length))" -ForegroundColor DarkGray
    } else {
        Write-Host $text -ForegroundColor Gray
    }
    
    # Search for date patterns
    Write-Host "`n=== Searching for Date Patterns in OCR Text ===" -ForegroundColor Yellow
    $datePatterns = @(
        "05 Nov 2025",
        "05 Dec 2025",
        "Nov 2025",
        "Dec 2025",
        "2025",
        "Billing Period",
        "Period",
        "0517",
        "5241"
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
    Write-Host "No OCR text in response" -ForegroundColor Yellow
}

Write-Host "`n=== Verification ===" -ForegroundColor Cyan
        if ($json.electricityUsage -and [math]::Abs($json.electricityUsage - 517) -lt 10) {
            Write-Host "✓ Electricity usage is close to expected (517 kWh)" -ForegroundColor Green
        } else {
            Write-Host "⚠ Electricity: Expected ~517 kWh, Got $($json.electricityUsage)" -ForegroundColor Yellow
        }
        
        if ($json.waterUsage -and [math]::Abs($json.waterUsage - 8.2) -lt 1) {
            Write-Host "✓ Water usage is close to expected (8.2 m³)" -ForegroundColor Green
        } else {
            Write-Host "⚠ Water: Expected ~8.2 m³, Got $($json.waterUsage)" -ForegroundColor Yellow
        }
        
        if ($json.gasUsage -eq 0) {
            Write-Host "✓ Gas usage matches expected (0)" -ForegroundColor Green
        } else {
            Write-Host "⚠ Gas: Expected 0, Got $($json.gasUsage)" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "`n✗ Failed to parse JSON: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Remove-Item $tempFile -ErrorAction SilentlyContinue
} else {
    Write-Host "✗ No response file created" -ForegroundColor Red
}
