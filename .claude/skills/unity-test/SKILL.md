---
name: unity-test
description: Use to run Unity Edit Mode tests (and optional code coverage) from the command line for this project, and to read the results. Use after implementing Core logic or fixing tests.
---

# Run Unity Edit Mode Tests (headless)

## Preconditions

- **The Unity editor must be CLOSED** for this project — batch mode fails with "already open in another instance" otherwise. If it fails this way, ask the user to close Unity, or ask the user to run tests via Editor: `Window > General > Test Runner > EditMode > Run All`.
- Editor exe: `C:\Program Files\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe`

## Run tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/run-tests.ps1
```

With coverage (requires `com.unity.testtools.codecoverage` package):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/run-tests.ps1 -coverage
```

## What the script does

- Runs `Unity.exe -projectPath . -batchmode -testPlatform editmode -runTests -testResults TestResults/editmode.xml -logFile TestResults/unity.log`
- With `-coverage`: adds `-debugCodeOptimization -enableCodeCoverage -coverageResultsPath TestResults/coverage -coverageOptions "generateAdditionalMetrics;generateHtmlReport;assemblyFilters:+Game.Core"` (per homework PDF §6; `-debugCodeOptimization` is required or measurement is wrong)
- Parses the NUnit XML and prints pass/fail counts; exits non-zero on any failure.
- With coverage: parses the coverage summary and fails if Game.Core line coverage < 90%.

## Reading results

- `TestResults/editmode.xml` — NUnit3 result. `result="Passed"` on `<test-run>` = all green.
- `TestResults/unity.log` — compile errors appear here when tests do not even run. Search for `CS\d\d\d\d` errors first when the run fails.
- `TestResults/coverage/Report/index.html` — coverage HTML report.

## Rules

- Never "fix" a red test by weakening or deleting its Assert. Fix the Core logic.
- Timeout: give the Bash/PowerShell call at least 300000 ms — a cold batch-mode run (import + compile + tests) can take several minutes.
