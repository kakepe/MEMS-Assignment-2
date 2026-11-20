param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $workspace
try {
    if (-not $NoBuild) {
        dotnet build MEMS.sln --no-restore
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    # Existing Aspire CLI and trusted development certificate; keep the original resource configuration.
    aspire run --apphost src/MEMS.AppHost/MEMS.AppHost.csproj --no-build --launch-profile https --non-interactive --nologo
} finally { Pop-Location }
