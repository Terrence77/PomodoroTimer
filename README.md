# WinUI AI-Driven Development Template

A minimal starter for building a **WinUI 3** desktop app with **Claude Code**, using a
simple **Issue → PR** workflow.

> This template targets **Claude Code** (`CLAUDE.md` and `.claude/commands/`). The WinUI agent
> plugin itself also works with GitHub Copilot CLI and OpenAI Codex, but the `/kickoff` and
> `/wrapup` commands here are Claude Code slash commands — adapt them for other agents.

## What's inside

| File | Purpose |
|---|---|
| `CLAUDE.md` | Rules the agent follows every session |
| `.claude/commands/kickoff.md` | `/kickoff <issue>` — read the issue, branch, propose a plan |
| `.claude/commands/wrapup.md` | `/wrapup` — commit, push, open a Draft PR (`Closes #`) |
| `.github/ISSUE_TEMPLATE/feature.md` | A feature issue with acceptance criteria |
| `.github/ISSUE_TEMPLATE/bug.md` | A bug report with steps to reproduce and expected behavior |

No CI, auto-review, or release automation is included — this template is intentionally minimal.

## How to use

1. Create a new repository from this template (GitHub "Use this template", or
   `gh repo create <name> --template <owner>/winui-ai-dev-template --public --clone`).
2. Install the WinUI agent plugin for Claude Code (see Microsoft Learn: *AI-assisted Windows development*).
3. Open an Issue with the Feature template and write its **acceptance criteria** (what, not how).
4. Run `/kickoff <issue-number>`, review the plan, and let the agent implement.
5. Run `/wrapup`, then review the Draft PR, run the app once yourself, and **squash-merge**.

The loop is: **Issue → `/kickoff` → implement → `/wrapup` → review → squash merge**.

## Running tests

**Unit tests** (headless, no UI) — xUnit tests for the UI-free logic in `PomodoroTimer.Core`:

```powershell
dotnet test
```

**UI tests** — drive the real app through UI Automation with the WinApp CLI (`winapp ui`).
They need the WinApp CLI (0.7+) on `PATH` and an interactive, unlocked desktop, so they are
kept out of `dotnet test`:

```powershell
.\RunUITests.ps1            # optional: -Configuration Release
```

The script builds and launches the app, runs `tests\UITests\TimerPage.UITests.ps1`, always
closes the app afterwards, and exits non-zero if any test fails. Results
(`test-results.json` and screenshots) are written to `tests\UITests\TestResults\` (git-ignored).
Avoid using the mouse or keyboard while the UI tests run.

## Packaging

Build a signed **Release MSIX** (self-contained: it carries the .NET runtime and the Windows App
SDK runtime, so nothing else needs to be installed). Requires the WinApp CLI (0.7+) on `PATH`:

```powershell
.\Package.ps1               # optional: -Platform ARM64
```

The package is written to `artifacts\PomodoroTimer_<version>_<arch>.msix` (git-ignored).

The first run creates a self-signed development certificate for the manifest Publisher
(`CN=Terrence77`) in `certs\` (git-ignored): `devcert.pfx`, its public key `devcert.cer`, and a
random password in `devcert.password.txt`. You can supply the password yourself with
`-CertPassword` or the `POMODORO_CERT_PASSWORD` environment variable instead. Never commit
anything from `certs\`. The script does not touch any Windows certificate store.

**Trust the certificate** (once per machine, in an **elevated** PowerShell):

```powershell
Import-Certificate -FilePath .\certs\devcert.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

**Install** (first remove the development registration made by `BuildAndRun.ps1` /
`RunUITests.ps1`, which has the same identity):

```powershell
Get-AppxPackage -Name 4AA179C1-B255-4BAE-8064-EE344466D710 |
    Where-Object { $_.Publisher -eq 'CN=Terrence77' -and $_.IsDevelopmentMode } |
    Remove-AppxPackage
Add-AppxPackage -Path .\artifacts\PomodoroTimer_1.0.0.0_x64.msix
```

**Uninstall:**

```powershell
Get-AppxPackage -Name 4AA179C1-B255-4BAE-8064-EE344466D710 | Remove-AppxPackage
```

The installed MSIX and the development registration share the same package identity, so
uninstall the MSIX before running `BuildAndRun.ps1` or `RunUITests.ps1` again.

To stop trusting the certificate later, remove it from `Cert:\LocalMachine\TrustedPeople`
(elevated): `Get-ChildItem Cert:\LocalMachine\TrustedPeople | Where-Object Subject -eq 'CN=Terrence77' | Remove-Item`.
