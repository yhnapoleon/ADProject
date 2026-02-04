# 检查用户42的出行记录和总碳排放
$connectionString = "Server=tcp:ecolens.database.windows.net,1433;Initial Catalog=ecolens;User ID=ecolensadmin;Password=EcoLens2025!;Encrypt=True;TrustServerCertificate=False;"
$sqlScriptPath = "E:\OneDrive\Desktop\AD\ADProject\.NET\EcoLens.Api\Migrations\check-travel-emission-user-42.sql"

Write-Host "Checking travel emission for User ID 42..." -ForegroundColor Yellow

try {
    $sqlScript = Get-Content $sqlScriptPath -Raw
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 120
    $reader = $command.ExecuteReader()

    Write-Host "`nResults:`n" -ForegroundColor Green
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
}
catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
