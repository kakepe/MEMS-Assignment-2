# Read-only SQL inspection of the existing intended Assignment 2 container/database.
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $workspace
$connection = $null
try {
    $containerId = & docker inspect mems-assignment2-sql --format '{{.Id}}'
    if ($LASTEXITCODE -ne 0 -or $containerId -ne 'e7fd02edfdaa499fb8c6ca179b91c3e680cd069787270457bdda608932ec7a26') { throw 'Unexpected SQL container; inspection stopped.' }
    $ports = (& docker inspect mems-assignment2-sql --format '{{json .NetworkSettings.Ports}}') | ConvertFrom-Json
    $port = $ports.'1433/tcp'[0].HostPort
    if (-not $port) { throw 'The existing SQL container must be running.' }
    $secrets = Get-Content -LiteralPath .local/apphost-secrets.json -Raw | ConvertFrom-Json
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source'] = "127.0.0.1,$port"
    $builder['Initial Catalog'] = 'MEMSAssignment2'
    $builder['User ID'] = 'sa'
    $builder['Password'] = $secrets.Parameters.'mems-sql-password'
    $builder['Encrypt'] = $true
    $builder['TrustServerCertificate'] = $true
    $connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    $connection.Open()
    function Read-Strings([string]$sql) {
        $command = $connection.CreateCommand(); $command.CommandText = $sql
        $reader = $command.ExecuteReader()
        try { while ($reader.Read()) { $reader.GetString(0) } } finally { $reader.Dispose(); $command.Dispose() }
    }
    $tables = @(Read-Strings 'SELECT name FROM sys.tables WHERE is_ms_shipped=0 ORDER BY name')
    $expected = @('__EFMigrationsHistory','AspNetRoleClaims','AspNetRoles','AspNetUserClaims','AspNetUserLogins','AspNetUserRoles','AspNetUserTokens','AspNetUsers','Candidates','Municipalities','Positions','SeedHistory')
    if (@(Compare-Object $tables $expected).Count) { throw 'Unexpected SQL table set.' }
    $migrations = @(Read-Strings 'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId')
    $expectedMigrations = @('20261116003601_InitialSqlServerIdentityTenancy','20261116004505_FixHyphenatedSlugsAndAssetNames')
    if (@(Compare-Object $migrations $expectedMigrations).Count) { throw 'Unexpected migration history.' }
    $choiceColumns = @(Read-Strings "SELECT OBJECT_NAME(object_id)+'.'+name FROM sys.columns WHERE name LIKE '%ballot%' OR name LIKE '%vote%' OR name LIKE '%selection%'")
    if ($choiceColumns.Count) { throw 'Unexpected vote/selection column.' }
    $command = $connection.CreateCommand()
    $command.CommandText = 'SELECT (SELECT COUNT(*) FROM Municipalities),(SELECT COUNT(*) FROM Positions),(SELECT COUNT(*) FROM Candidates),(SELECT COUNT(*) FROM AspNetUsers),(SELECT COUNT(*) FROM AspNetRoles)'
    $reader = $command.ExecuteReader(); [void]$reader.Read()
    $counts = [ordered]@{ municipalities=$reader.GetInt32(0); positions=$reader.GetInt32(1); candidates=$reader.GetInt32(2); users=$reader.GetInt32(3); roles=$reader.GetInt32(4) }
    $reader.Dispose(); $command.Dispose()
    if ($counts.municipalities -ne 5 -or $counts.positions -ne 15 -or $counts.candidates -ne 30 -or $counts.users -ne 5 -or $counts.roles -ne 7) { throw 'Unexpected final baseline counts.' }
    [ordered]@{ passed=$true; database='MEMSAssignment2'; tables=$tables; migrations=$migrations; voteOrSelectionColumns=$choiceColumns; counts=$counts } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath artifacts/phase6/schema-verification.json -Encoding UTF8
    Write-Output 'PASS: exactly 12 existing tables, two unchanged migrations, no ballot/vote/selection columns, and baseline 5/15/30/5/7 counts.'
} finally { if ($connection) { $connection.Dispose() }; Pop-Location }
