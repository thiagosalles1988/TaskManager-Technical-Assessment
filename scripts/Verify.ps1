param([string]$SqlConnectionString, [switch]$RequireSql)
$ErrorActionPreference = 'Stop'
function Invoke-TaskManagerNpm {
    param([string[]]$NpmArguments)
    # Windows PowerShell can turn native stderr warnings into terminating errors when redirected.
    # Preserve output, but use the native exit code to determine command success.
    $taskManagerPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & npm @NpmArguments
        $taskManagerNpmExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $taskManagerPreference }
    if ($taskManagerNpmExit -ne 0) { throw "npm $($NpmArguments -join ' ') failed (exit $taskManagerNpmExit)." }
}
$taskManagerRoot = Split-Path -Parent $PSScriptRoot
if ($SqlConnectionString) { $env:TaskManagerTests__ConnectionString = $SqlConnectionString }
if ($RequireSql -and -not $env:TaskManagerTests__ConnectionString) {
    throw 'SQL verification is required. Supply -SqlConnectionString or TaskManagerTests__ConnectionString.'
}
Push-Location $taskManagerRoot
try {
    dotnet restore TaskManager.slnx
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    dotnet build TaskManager.slnx --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet test TaskManager.slnx --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    Push-Location taskmanager-web
    try {
        Invoke-TaskManagerNpm -NpmArguments @('ci')
        Invoke-TaskManagerNpm -NpmArguments @('test')
        Invoke-TaskManagerNpm -NpmArguments @('run', 'build')
    } finally { Pop-Location }
    if (-not $env:TaskManagerTests__ConnectionString) {
        Write-Warning 'SQL integration tests were skipped. Supply -SqlConnectionString to verify persistence and HTTP integration.'
    }
} finally { Pop-Location }
