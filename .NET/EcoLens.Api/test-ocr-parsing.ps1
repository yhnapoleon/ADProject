# Test OCR Text Parsing with Real Bill Format
# This simulates what OCR would extract from the SP Services bill

$baseUrl = "http://localhost:5133"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Test OCR Text Parsing" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Simulated OCR text from SP Services bill
# Based on the actual bill structure
$simulatedOcrText = @"
SP Services Ltd
Dec 2025 PDF Bill
Reference Number: 218-000777-00389-5988
Billing Period: 05 Nov 2025 - 05 Dec 2025
Bill Date: 07 Dec 2025

Electricity Services
Usage: 517 kWh
Total Charge: $142.43

Gas Services
by City Energy Pte. Ltd.
Usage: 0 kWh
Total Charge: $0.00

Water Services
by Public Utilities Board
Usage: 8.2 Cu M
Total Charge: $26.53

Total Current Charges: $199.48
"@

Write-Host "`n=== Simulated OCR Text ===" -ForegroundColor Cyan
Write-Host $simulatedOcrText -ForegroundColor Gray

# Step 1: Login
Write-Host "`n=== Step 1: Login ===" -ForegroundColor Cyan
$timestamp = Get-Date -Format 'HHmmss'
$email = "test$timestamp@example.com"

$loginBody = @{
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    # Try login first
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    # If login fails, register
    $registerBody = @{
        username = "testuser$timestamp"
        email = $email
        password = "Test123!"
    } | ConvertTo-Json
    
    try {
        $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
        $token = $loginResponse.token
        Write-Host "✓ Registered and logged in" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Test the parser by creating a manual bill with expected extracted values
Write-Host "`n=== Step 2: Expected Extracted Values ===" -ForegroundColor Cyan
Write-Host "Based on the OCR text, the parser should extract:" -ForegroundColor Yellow
Write-Host "  Electricity: 517 kWh" -ForegroundColor White
Write-Host "  Water: 8.2 m³ (from '8.2 Cu M')" -ForegroundColor White
Write-Host "  Gas: 0 kWh" -ForegroundColor White
Write-Host "  Period: 05 Nov 2025 - 05 Dec 2025" -ForegroundColor White

Write-Host "`n=== Note ===" -ForegroundColor Cyan
Write-Host "To fully test OCR parsing, you need to:" -ForegroundColor Yellow
Write-Host "1. Upload the actual bill image using the upload API" -ForegroundColor White
Write-Host "2. The OCR service will extract text from the image" -ForegroundColor White
Write-Host "3. The parser will extract data from the OCR text" -ForegroundColor White
Write-Host "4. The system will calculate carbon emissions" -ForegroundColor White

Write-Host "`n=== Current Parser Capabilities ===" -ForegroundColor Cyan
Write-Host "The parser should be able to extract:" -ForegroundColor Yellow
Write-Host "  ✓ Electricity: '517 kWh' format" -ForegroundColor Green
Write-Host "  ⚠ Water: '8.2 Cu M' format (may need enhancement)" -ForegroundColor Yellow
Write-Host "  ✓ Gas: '0 kWh' format" -ForegroundColor Green
Write-Host "  ✓ Dates: '05 Nov 2025 - 05 Dec 2025' format" -ForegroundColor Green

Write-Host "`n=== Recommendation ===" -ForegroundColor Cyan
Write-Host "To test with actual OCR:" -ForegroundColor Yellow
Write-Host "1. Save the bill image to a file (e.g., bill.jpg)" -ForegroundColor White
Write-Host "2. Run: .\upload-bill-test.ps1 -FilePath `"path\to\bill.jpg`"" -ForegroundColor White
Write-Host "3. Check if the extracted values match the expected values" -ForegroundColor White
