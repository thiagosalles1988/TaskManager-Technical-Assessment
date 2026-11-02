param([string]$ConnectionString)
$ErrorActionPreference = 'Stop'
$taskManagerRoot = Split-Path -Parent $PSScriptRoot
if ($ConnectionString) { $env:ConnectionStrings__TaskManager = $ConnectionString }
if (-not $env:Jwt__SigningKey) {
    $taskManagerBytes = New-Object byte[] 64
    $taskManagerRng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $taskManagerRng.GetBytes($taskManagerBytes) }
    finally { $taskManagerRng.Dispose() }
    $env:Jwt__SigningKey = [Convert]::ToBase64String($taskManagerBytes)
    Write-Host 'Generated a development signing key for this shell. A fresh shell generates a new key; an existing environment key is reused.'
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__InitializeOnStart = 'true'
$env:Database__SeedDemo = 'true'
dotnet run --project (Join-Path $taskManagerRoot 'src/TaskManager.Api') --launch-profile http
if ($LASTEXITCODE -ne 0) { throw 'API startup failed. Check SQL connectivity and configuration.' }
