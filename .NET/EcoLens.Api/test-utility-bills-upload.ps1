# Utility Bills Upload and OCR Test Script
# This script tests file upload and OCR functionality
# Note: Requires actual bill image/PDF files for testing

$baseUrl = "http://localhost:5133"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Utility Bills Upload & OCR Test" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Register and Login
Write-Host "`n=== Step 1: Register and Login ===" -ForegroundColor Cyan
$timestamp = Get-Date -Format 'HHmmss'
$username = "testuser$timestamp"
$email = "test$timestamp@example.com"

$registerBody = @{
    username = $username
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✓ Registered: $username ($email)" -ForegroundColor Green
} catch {
    Write-Host "✗ Registration failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$loginBody = @{
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful. Token obtained (length: $($token.Length))" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
}

# Step 2: Test File Validation - Invalid File Type
Write-Host "`n=== Step 2: Test File Validation (Invalid File Type) ===" -ForegroundColor Cyan
try {
    # Create a temporary text file
    $tempFile = [System.IO.Path]::GetTempFileName() + ".txt"
    "Test content" | Out-File -FilePath $tempFile -Encoding UTF8
    
    $fileContent = Get-Content $tempFile -Raw -Encoding Byte
    $boundary = [System.Guid]::NewGuid().ToString()
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"test.txt`"",
        "Content-Type: text/plain",
        "",
        [System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($fileContent),
        "--$boundary--"
    )
    $body = $bodyLines -join "`r`n"
    $bodyBytes = [System.Text.Encoding]::GetEncoding('iso-8859-1').GetBytes($body)
    
    $uploadHeaders = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }
    
    $uploadResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/upload" -Method POST -Headers $uploadHeaders -Body $bodyBytes
    Write-Host "✗ Should have failed but didn't" -ForegroundColor Red
    Remove-Item $tempFile -ErrorAction SilentlyContinue
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✓ Correctly rejected invalid file type" -ForegroundColor Green
    } else {
        Write-Host "⚠ Unexpected error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

# Step 3: Test File Validation - File Too Large
Write-Host "`n=== Step 3: Test File Validation (File Too Large) ===" -ForegroundColor Cyan
Write-Host "  Note: Creating a 11MB test file..." -ForegroundColor Gray
try {
    # Create a file larger than 10MB
    $tempFile = [System.IO.Path]::GetTempFileName() + ".jpg"
    $largeContent = New-Object byte[] (11 * 1024 * 1024)  # 11MB
    [System.IO.File]::WriteAllBytes($tempFile, $largeContent)
    
    $fileContent = [System.IO.File]::ReadAllBytes($tempFile)
    $boundary = [System.Guid]::NewGuid().ToString()
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"large.jpg`"",
        "Content-Type: image/jpeg",
        "",
        [System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($fileContent),
        "--$boundary--"
    )
    $body = $bodyLines -join "`r`n"
    $bodyBytes = [System.Text.Encoding]::GetEncoding('iso-8859-1').GetBytes($body)
    
    $uploadHeaders = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }
    
    $uploadResponse = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/upload" -Method POST -Headers $uploadHeaders -Body $bodyBytes
    Write-Host "✗ Should have failed but didn't" -ForegroundColor Red
    Remove-Item $tempFile -ErrorAction SilentlyContinue
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✓ Correctly rejected file that is too large" -ForegroundColor Green
    } else {
        Write-Host "⚠ Unexpected error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

# Step 4: Instructions for Manual OCR Testing
Write-Host "`n=== Step 4: Manual OCR Testing Instructions ===" -ForegroundColor Cyan
Write-Host "To test OCR functionality with actual bill images:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Prepare a bill image file (JPG/PNG) or PDF:" -ForegroundColor White
Write-Host "   - Supported formats: JPG, PNG, BMP, WEBP, PDF" -ForegroundColor Gray
Write-Host "   - File size: Max 10MB" -ForegroundColor Gray
Write-Host "   - Recommended: Clear image of Singapore utility bill (SP Group, PUB)" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Use the following PowerShell command to upload:" -ForegroundColor White
Write-Host '   $filePath = "path\to\your\bill.jpg"' -ForegroundColor Cyan
Write-Host '   $fileBytes = [System.IO.File]::ReadAllBytes($filePath)' -ForegroundColor Cyan
Write-Host '   $boundary = [System.Guid]::NewGuid().ToString()' -ForegroundColor Cyan
Write-Host '   $fileName = [System.IO.Path]::GetFileName($filePath)' -ForegroundColor Cyan
Write-Host '   $fileContent = [System.Convert]::ToBase64String($fileBytes)' -ForegroundColor Cyan
Write-Host '   $body = @"' -ForegroundColor Cyan
Write-Host "--$boundary" -ForegroundColor Cyan
Write-Host "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"" -ForegroundColor Cyan
Write-Host "Content-Type: image/jpeg" -ForegroundColor Cyan
Write-Host "" -ForegroundColor Cyan
Write-Host "$fileContent" -ForegroundColor Cyan
Write-Host "--$boundary--" -ForegroundColor Cyan
Write-Host '"@' -ForegroundColor Cyan
Write-Host '   $headers = @{' -ForegroundColor Cyan
Write-Host '       "Authorization" = "Bearer $token"' -ForegroundColor Cyan
Write-Host '       "Content-Type" = "multipart/form-data; boundary=$boundary"' -ForegroundColor Cyan
Write-Host '   }' -ForegroundColor Cyan
Write-Host '   $response = Invoke-RestMethod -Uri "$baseUrl/api/UtilityBill/upload" -Method POST -Headers $headers -Body $body' -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Or use a tool like Postman or curl:" -ForegroundColor White
Write-Host "   POST http://localhost:5133/api/UtilityBill/upload" -ForegroundColor Cyan
Write-Host "   Headers: Authorization: Bearer {token}" -ForegroundColor Cyan
Write-Host "   Body: form-data, key: file, value: [select your bill file]" -ForegroundColor Cyan
Write-Host ""
Write-Host "4. Expected response:" -ForegroundColor White
Write-Host "   - If OCR succeeds: Returns bill data with extracted usage and calculated carbon emission" -ForegroundColor Gray
Write-Host "   - If OCR fails: Returns 400 error with message to use manual input" -ForegroundColor Gray
Write-Host "   - Response includes: billType, usage data, carbon emission, OCR confidence" -ForegroundColor Gray

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "Note: OCR testing requires actual bill images. Use the instructions above to test manually." -ForegroundColor Yellow
