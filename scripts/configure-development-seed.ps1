$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$local = Join-Path $workspace '.local'
$target = Join-Path $local 'identity-seed.json'
if (Test-Path -LiteralPath $target) { Write-Output 'Existing local seed credential retained.'; return }
New-Item -ItemType Directory -Path $local -Force | Out-Null
$secure = Read-Host 'Enter the rubric development/demo password (local use only)' -AsSecureString
$buffer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($buffer)
    if ([string]::IsNullOrWhiteSpace($password)) { throw 'A non-empty development credential is required.' }
    $json = @{ Parameters = @{ 'mems-identity-seed-password' = $password } } | ConvertTo-Json
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($json)
    $stream = [IO.File]::Open($target, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
    Write-Output 'Local development seed credential configured. No existing password or SQL secret was overwritten.'
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($buffer)
    $password = $null
    $secure.Dispose()
}
