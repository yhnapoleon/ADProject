# 重新计算所有用户的总碳排放
$connectionString = "Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;User ID=ecolensadmin;Password=EcoLens2025!;Encrypt=True;TrustServerCertificate=False;"
$sqlScriptPath = "E:\OneDrive\Desktop\AD\ADProject\.NET\EcoLens.Api\Migrations\recalculate-all-users-total-carbon-emission.sql"

Write-Host "Recalculating total carbon emission for all users..." -ForegroundColor Yellow

try {
    $sqlScript = Get-Content $sqlScriptPath -Raw
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 120
    $reader = $command.ExecuteReader()

    Write-Host "`nUpdate completed. Verification results:`n" -ForegroundColor Green
    do {
        while ($reader.Read()) {
            $row = New-Object PSObject
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $columnName = $reader.GetName($i)
                $columnValue = $reader.GetValue($i)
                $row | Add-Member -MemberType NoteProperty -Name $columnName -Value $columnValue
            }
            $row
        }
        Write-Host "---" -ForegroundColor Gray
    } while ($reader.NextResult())

    $reader.Close()
    $connection.Close()
    Write-Host "`nSUCCESS: Total carbon emission recalculated for all users!" -ForegroundColor Green
}
catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
