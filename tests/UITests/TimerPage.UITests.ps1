<#
.SYNOPSIS
    UI Automation tests for the Pomodoro Timer page, driven by `winapp ui`.

.DESCRIPTION
    Runs against an already-running instance of the app on the local desktop (identified by
    -AppPid). Normally invoked by RunUITests.ps1 at the repository root, which builds,
    launches, and closes the app.

    The tests never hardcode the Work duration: the initial remaining time is read from the
    UI, because durations are user-configurable in Settings. The tests do not change
    settings.

.PARAMETER AppPid
    Process ID of the running PomodoroTimer app.

.PARAMETER ResultsDirectory
    Folder for test-results.json and screenshots.

.OUTPUTS
    Exit code 0 when all tests pass, 1 otherwise.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, [int]::MaxValue)][int] $AppPid,
    [string] $ResultsDirectory
)

if (-not $ResultsDirectory) {
    $ResultsDirectory = Join-Path $PSScriptRoot ('TestResults\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$results = [Collections.Generic.List[object]]::new()
$screenshots = [Collections.Generic.List[string]]::new()

# Shared between tests: the remaining time shown at a clean Work-phase start.
$script:InitialRemaining = $null

function Invoke-WinAppChecked {
    param([string[]] $Arguments)
    $global:LASTEXITCODE = $null
    $output = @(& winapp @Arguments)
    $code = $global:LASTEXITCODE
    if ($null -eq $code -or $code -ne 0) {
        throw "winapp $($Arguments -join ' ') failed (exit $code). $($output -join "`n")"
    }
    $output
}

function Invoke-Ui {
    param([string[]] $Arguments)
    Invoke-WinAppChecked (@('ui') + $Arguments + @('-a', "$AppPid"))
}

function Get-UiText {
    param([string] $AutomationId)
    $json = (Invoke-Ui @('get-value', $AutomationId, '--json')) -join "`n"
    ($json | ConvertFrom-Json).text
}

function Test-UiEnabled {
    param([string] $AutomationId)
    $json = (Invoke-Ui @('get-property', $AutomationId, '-p', 'IsEnabled', '--json')) -join "`n"
    ($json | ConvertFrom-Json).properties.IsEnabled -eq 'True'
}

function Assert-UiEnabled {
    param([string] $AutomationId, [bool] $Expected, [int] $TimeoutMs = 3000)
    try {
        Invoke-Ui @('wait-for', $AutomationId, '-p', 'IsEnabled', '--value', "$Expected", '-t', "$TimeoutMs") | Out-Null
    } catch {
        throw "Expected $AutomationId IsEnabled=$Expected within $TimeoutMs ms."
    }
}

# Converts "mm:ss" (or "h:mm:ss") into total seconds.
function ConvertTo-Seconds {
    param([string] $Text)
    if ($Text -notmatch '^\d{1,3}(:\d{2}){1,2}$') {
        throw "Remaining time '$Text' is not in mm:ss format."
    }
    $total = 0
    foreach ($part in $Text.Split(':')) { $total = ($total * 60) + [int]$part }
    $total
}

function Save-Screenshot {
    param([string] $Name)
    $path = Join-Path $ResultsDirectory $Name
    Invoke-Ui @('screenshot', '-o', $path) | Out-Null
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Screenshot was not written: $path" }
    $screenshots.Add($path)
}

function Test-UI {
    param([string] $Name, [scriptblock] $Script)
    try {
        & $Script | Out-Null
        $results.Add([ordered]@{ name = $Name; status = 'PASS' })
        Write-Host "[PASS] $Name" -ForegroundColor Green
    } catch {
        $results.Add([ordered]@{ name = $Name; status = 'FAIL'; detail = "$_" })
        Write-Host "[FAIL] $Name`n       $_" -ForegroundColor Red
    }
}

try {
    $ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
    New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null

    # Setup: show the Timer page and bring the timer to a clean, idle Work-phase start.
    Invoke-Ui @('wait-for', 'NavTimer', '-t', '15000') | Out-Null
    Invoke-Ui @('invoke', 'NavTimer') | Out-Null
    Invoke-Ui @('wait-for', 'RemainingTimeText', '-t', '5000') | Out-Null
    if (Test-UiEnabled 'ResetButton') {
        Invoke-Ui @('invoke', 'ResetButton') | Out-Null
    }
    Assert-UiEnabled 'ResetButton' $false
    Assert-UiEnabled 'StartButton' $true

    Test-UI 'Start decreases remaining time' {
        $script:InitialRemaining = Get-UiText 'RemainingTimeText'
        $initialSeconds = ConvertTo-Seconds $script:InitialRemaining

        Invoke-Ui @('invoke', 'StartButton') | Out-Null
        Assert-UiEnabled 'PauseButton' $true
        Assert-UiEnabled 'StartButton' $false

        # Poll for up to ~5 s (the timer ticks once per second) and stop early once the
        # value has strictly decreased.
        $deadline = [DateTime]::UtcNow.AddSeconds(5)
        do {
            Start-Sleep -Milliseconds 500
            $current = Get-UiText 'RemainingTimeText'
            $currentSeconds = ConvertTo-Seconds $current
        } while ($currentSeconds -ge $initialSeconds -and [DateTime]::UtcNow -lt $deadline)

        Save-Screenshot '01-running.png'
        if ($currentSeconds -ge $initialSeconds) {
            throw "Remaining time did not decrease: initial '$($script:InitialRemaining)', now '$current'."
        }
    }

    Test-UI 'Reset returns to initial value' {
        if (-not $script:InitialRemaining) { throw 'Initial remaining time was not captured.' }
        # Make sure the timer is running so Reset is exercised from a non-initial state.
        if (Test-UiEnabled 'StartButton') {
            Invoke-Ui @('invoke', 'StartButton') | Out-Null
            Assert-UiEnabled 'PauseButton' $true
        }

        Invoke-Ui @('invoke', 'ResetButton') | Out-Null
        try {
            Invoke-Ui @('wait-for', 'RemainingTimeText', '--value', $script:InitialRemaining, '-t', '3000') | Out-Null
        } catch {
            throw "Remaining time is '$(Get-UiText 'RemainingTimeText')' after Reset; expected '$($script:InitialRemaining)'."
        }

        # Only the current phase's badge is in the tree; collapsed elements are not exposed.
        try { Invoke-Ui @('wait-for', 'WorkPhaseText', '-t', '3000') | Out-Null }
        catch { throw 'Work phase badge is not visible after Reset.' }
        try { Invoke-Ui @('wait-for', 'BreakPhaseText', '--gone', '-t', '3000') | Out-Null }
        catch { throw 'Break phase badge is still visible after Reset.' }

        Assert-UiEnabled 'StartButton' $true
        Assert-UiEnabled 'PauseButton' $false

        # Stays at the initial value (the countdown really stopped).
        Start-Sleep -Milliseconds 1500
        $after = Get-UiText 'RemainingTimeText'
        if ($after -ne $script:InitialRemaining) {
            throw "Timer kept running after Reset: expected '$($script:InitialRemaining)', now '$after'."
        }
        Save-Screenshot '02-after-reset.png'
    }
} catch {
    $results.Add([ordered]@{ name = 'Test setup'; status = 'FAIL'; detail = "$_" })
    Write-Host "[FAIL] Test setup`n       $_" -ForegroundColor Red
}

$failed = @($results | Where-Object { $_.status -eq 'FAIL' }).Count
$report = [ordered]@{
    appPid      = $AppPid
    passed      = $results.Count - $failed
    failed      = $failed
    results     = @($results.ToArray())
    screenshots = @($screenshots.ToArray())
}
$resultsFile = Join-Path $ResultsDirectory 'test-results.json'
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resultsFile -Encoding utf8
Write-Host "Passed: $($report.passed) | Failed: $failed | Results: $resultsFile"

if ($failed -gt 0 -or $results.Count -eq 0) { exit 1 }
exit 0
