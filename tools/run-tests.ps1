# run-tests.ps1
# Unity Edit Mode テストをバッチモードで実行するスクリプト
# 使い方:
#   powershell -File tools/run-tests.ps1              # テストのみ
#   powershell -File tools/run-tests.ps1 -coverage    # テスト + Game.Core カバレッジ (90% 未満で fail)

param(
    [switch]$coverage
)

$unity_exe = "C:\Program Files\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe"
$project_dir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$results_dir = Join-Path $project_dir "TestResults"
$test_xml = Join-Path $results_dir "editmode.xml"
$log_file = Join-Path $results_dir "unity.log"

if (-not (Test-Path $unity_exe)) {
    Write-Error "Unity editor not found: $unity_exe"
    exit 1
}

New-Item -ItemType Directory -Force -Path $results_dir | Out-Null
if (Test-Path $test_xml) { Remove-Item $test_xml -Force }

# バッチモード引数を組み立てる
$unity_args = @(
    "-projectPath", $project_dir,
    "-batchmode",
    "-testPlatform", "editmode",
    "-runTests",
    "-testResults", $test_xml,
    "-logFile", $log_file
)

if ($coverage) {
    # 課題PDF 6章: -debugCodeOptimization が無いと計測が正しく行われない
    $coverage_dir = Join-Path $results_dir "coverage"
    $unity_args += @(
        "-debugCodeOptimization",
        "-enableCodeCoverage",
        "-coverageResultsPath", $coverage_dir,
        "-coverageOptions", "generateAdditionalMetrics;generateHtmlReport;assemblyFilters:+Game.Core"
    )
}

Write-Host "Running Unity Edit Mode tests (this may take a few minutes)..."
$proc = Start-Process -FilePath $unity_exe -ArgumentList $unity_args -Wait -PassThru -NoNewWindow
Write-Host "Unity exit code: $($proc.ExitCode)"

# テスト結果 XML を解析する
if (-not (Test-Path $test_xml)) {
    Write-Error "No test result XML. Editor may be open, or compile error. Check $log_file"
    exit 1
}

[xml]$xml = Get-Content $test_xml
$run = $xml."test-run"
Write-Host ("Tests: total={0} passed={1} failed={2} result={3}" -f $run.total, $run.passed, $run.failed, $run.result)

if ($run.result -ne "Passed") {
    # 失敗したテスト名を表示する
    $failed = $xml.SelectNodes("//test-case[@result='Failed']")
    foreach ($t in $failed) { Write-Host ("  FAILED: {0}" -f $t.fullname) }
    exit 1
}

# カバレッジ閾値チェック（制約E: コア層 90% 以上）
if ($coverage) {
    $summary = Get-ChildItem -Path (Join-Path $results_dir "coverage") -Filter "Summary.xml" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $summary) {
        Write-Error "Coverage Summary.xml not found. Is com.unity.testtools.codecoverage installed?"
        exit 1
    }
    [xml]$cov = Get-Content $summary.FullName
    $line_rate = [double]$cov.CoverageSession.Summary.Linecoverage
    Write-Host ("Game.Core line coverage: {0}%" -f $line_rate)
    if ($line_rate -lt 90) {
        Write-Error "Coverage below 90% threshold."
        exit 1
    }
}

Write-Host "ALL GREEN"
exit 0
