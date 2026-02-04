# 为 ID 42 的用户添加 100000 步数
$connectionString = "Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;User ID=ecolensadmin;Password=EcoLens2025!;Encrypt=True;TrustServerCertificate=False;"
$sqlScriptPath = "E:\OneDrive\Desktop\AD\ADProject\.NET\EcoLens.Api\Migrations\add-steps-user-42.sql"

Write-Host "Adding 100000 steps for User ID 42..." -ForegroundColor Yellow

try {
    $sqlScript = Get-Content $sqlScriptPath -Raw
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 120
    $reader = $command.ExecuteReader()

    Write-Host "`nResult:`n" -ForegroundColor Green
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
    } while ($reader.NextResult())

    $reader.Close()
    $connection.Close()
    Write-Host "`nSUCCESS: Steps added for User ID 42!" -ForegroundColor Green
}
catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
