# 为所有用户填充碳减排与积分
$connectionString = "Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;User ID=ecolensadmin;Password=EcoLens2025!;Encrypt=True;TrustServerCertificate=False;"
$sqlScriptPath = Join-Path $PSScriptRoot "seed-all-users-carbon-saved-and-points.sql"

Write-Host "Seeding TotalCarbonSaved and CurrentPoints for all users..." -ForegroundColor Yellow

try {
    $sqlScript = Get-Content $sqlScriptPath -Raw -Encoding UTF8
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Add_InfoMessage({ param($sender, $e) Write-Host $e.Message -ForegroundColor Cyan })
    $connection.FireInfoMessageEventOnUserErrors = $true
    $connection.Open()
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 60
    [void]$command.ExecuteNonQuery()
    $connection.Close()
    Write-Host "`nSUCCESS: Done." -ForegroundColor Green
}
catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') { $connection.Close() }
}
