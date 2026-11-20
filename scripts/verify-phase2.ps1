param([ValidateSet('Verify','PrepareRestart','CompleteRestart','PreparePhase3','CompletePhase3')][string]$Mode = 'Verify')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $workspace
$names = @('ConnectionStrings__mems','Seed__DevelopmentPassword','Mems__LocalStorage','ASPNETCORE_ENVIRONMENT','DOTNET_ENVIRONMENT')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $bindings = (& docker inspect mems-assignment2-sql --format '{{json .NetworkSettings.Ports}}') | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect the existing Assignment 2 container.' }
    $hostPort = $bindings.'1433/tcp'[0].HostPort
    if (-not $hostPort) { throw 'Start the Assignment 2 SQL container first.' }
    $secrets = Get-Content -LiteralPath '.local/apphost-secrets.json' -Raw | ConvertFrom-Json
    $identity = Get-Content -LiteralPath '.local/identity-seed.json' -Raw | ConvertFrom-Json
    $connection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $connection['Data Source'] = "127.0.0.1,$hostPort"
    $connection['Initial Catalog'] = 'MEMSAssignment2'
    $connection['User ID'] = 'sa'
    $connection['Password'] = $secrets.Parameters.'mems-sql-password'
    $connection['Encrypt'] = $true
    $connection['TrustServerCertificate'] = $true
    $env:ConnectionStrings__mems = $connection.ConnectionString
    $env:Seed__DevelopmentPassword = $identity.Parameters.'mems-identity-seed-password'
    $env:Mems__LocalStorage = Join-Path $workspace '.local'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    $argument = @{ Verify='--verify-phase2'; PrepareRestart='--prepare-phase2-restart'; CompleteRestart='--complete-phase2-restart'; PreparePhase3='--prepare-phase3'; CompletePhase3='--complete-phase3' }[$Mode]
    dotnet src/MEMS.Api/bin/Debug/net10.0/MEMS.Api.dll $argument
    if ($LASTEXITCODE -ne 0) { throw 'Phase 2 database verification failed; inspect the sanitized error above.' }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    Pop-Location
}
