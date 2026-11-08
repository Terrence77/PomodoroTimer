<#
.SYNOPSIS
    Builds and launches the Pomodoro Timer app, runs the UI Automation tests, then closes it.

.DESCRIPTION
    Requires the WinApp CLI (0.7+) and an interactive, unlocked desktop: the tests drive the
    real app through UI Automation, so they are not part of `dotnet test`.

    Results (test-results.json and screenshots) are written under tests\UITests\TestResults\.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug.

.OUTPUTS
    Exit code 0 when all UI tests pass, non-zero otherwise.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) {
    Write-Error "WinApp CLI ('winapp') was not found on PATH. Install it (0.7 or later) and try again."
    exit 2
}

$project = Join-Path $PSScriptRoot 'src\PomodoroTimer\PomodoroTimer.csproj'
$testScript = Join-Path $PSScriptRoot 'tests\UITests\TimerPage.UITests.ps1'
$appProcess = $null
$exitCode = 1

try {
    Write-Host "Building and launching the app ($Configuration)..."
    $global:LASTEXITCODE = $null
    # winapp writes build progress to stderr. Under Windows PowerShell 5.1, when the caller
    # redirects stderr (2>&1, CI log capture), each such line becomes an error record, which
    # 'Stop' would turn into a terminating error. Rely on the exit code instead.
    $ErrorActionPreference = 'Continue'
    try {
        $runOutput = @(& winapp run $project -c $Configuration --detach --json | ForEach-Object { "$_" })
    } finally {
        $ErrorActionPreference = 'Stop'
    }
    if ($LASTEXITCODE -ne 0) {
        $runOutput | Write-Host
        throw "winapp run failed (exit $LASTEXITCODE)."
    }

    # Build output precedes the JSON document; the JSON starts at a line that is exactly '{'.
    $jsonStart = [Array]::LastIndexOf($runOutput, '{')
    if ($jsonStart -lt 0) {
        $runOutput | Write-Host
        throw 'Could not find the launch JSON in winapp run output.'
    }
    $launch = ($runOutput[$jsonStart..($runOutput.Count - 1)] -join "`n") | ConvertFrom-Json
    $appPid = [int]$launch.ProcessId
    if ($appPid -le 0) { throw 'winapp run did not report a ProcessId.' }
    $appProcess = Get-Process -Id $appPid -ErrorAction SilentlyContinue
    Write-Host "App launched (PID $appPid). Running UI tests..."

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $testScript -AppPid $appPid
    $exitCode = $LASTEXITCODE
} catch {
    Write-Host "UI test run failed: $_" -ForegroundColor Red
    $exitCode = 1
} finally {
    if ($appProcess -and -not $appProcess.HasExited) {
        Write-Host "Closing the app (PID $($appProcess.Id))..."
        Stop-Process -Id $appProcess.Id -Force -ErrorAction SilentlyContinue
        $appProcess.WaitForExit(10000) | Out-Null
    }
}

if ($exitCode -eq 0) {
    Write-Host 'UI tests passed.' -ForegroundColor Green
} else {
    Write-Host "UI tests FAILED (exit $exitCode)." -ForegroundColor Red
}
exit $exitCode
