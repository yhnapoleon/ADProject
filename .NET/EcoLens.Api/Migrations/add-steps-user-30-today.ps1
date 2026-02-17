# 为 ID 30 用户新增今日步数 2000000 步
$connectionString = "Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;User ID=ecolensadmin;Password=EcoLens2025!;Encrypt=True;TrustServerCertificate=False;"
$sqlScriptPath = Join-Path $PSScriptRoot "add-steps-user-30-today.sql"

Write-Host "Adding 2000000 steps for User ID 30 (today)..." -ForegroundColor Yellow

try {
    $sqlScript = Get-Content $sqlScriptPath -Raw -Encoding UTF8
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Add_InfoMessage({ param($sender, $e) Write-Host $e.Message -ForegroundColor Cyan })
    $connection.FireInfoMessageEventOnUserErrors = $true
    $connection.Open()
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 60
    $reader = $command.ExecuteReader()

    Write-Host "`nResult:" -ForegroundColor Green
    do {
        while ($reader.Read()) {
            $row = New-Object PSObject
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $row | Add-Member -MemberType NoteProperty -Name $reader.GetName($i) -Value $reader.GetValue($i)
            }
            $row
        }
    } while ($reader.NextResult())

    $reader.Close()
    $connection.Close()
    Write-Host "`nSUCCESS: Steps added for User ID 30." -ForegroundColor Green
}
catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') { $connection.Close() }
}
