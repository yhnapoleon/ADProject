# 为所有用户填充出行/食物/水电种子数据（直接执行到云库）
$connectionString = "Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;User ID=ecolensadmin;Password=EcoLens2025!;Encrypt=True;TrustServerCertificate=False;"
$sqlScriptPath = Join-Path $PSScriptRoot "seed-all-users-travel-food-utility.sql"

Write-Host "Seeding travel / food / utility data for all users..." -ForegroundColor Yellow

try {
    $sqlScript = Get-Content $sqlScriptPath -Raw -Encoding UTF8
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Add_InfoMessage({
        param($sender, $e)
        Write-Host $e.Message -ForegroundColor Cyan
    })
    $connection.FireInfoMessageEventOnUserErrors = $true
    $connection.Open()

    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 300
    [void]$command.ExecuteNonQuery()

    $connection.Close()
    Write-Host "`nSUCCESS: Seed completed." -ForegroundColor Green
}
catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
    }
}
