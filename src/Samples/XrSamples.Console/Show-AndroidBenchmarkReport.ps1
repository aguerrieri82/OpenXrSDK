$ErrorActionPreference = 'Stop'

$resultsRoot = 'D:\Projects\XrEditor'
$benchmarkRoot = Join-Path $resultsRoot 'AndroidBenchmarks'
$templatePath = Join-Path $PSScriptRoot 'BenchmarkReport.template.html'
$outputPath = Join-Path $benchmarkRoot 'report.html'

if (-not (Test-Path -LiteralPath $templatePath)) {
    throw "Benchmark report template not found: $templatePath"
}

$runFiles = @(Get-ChildItem -LiteralPath $benchmarkRoot -Filter 'run-*.json' -File -Recurse | Sort-Object FullName)
if ($runFiles.Count -eq 0) {
    throw "No benchmark run files found under: $benchmarkRoot"
}

$runs = foreach ($runFile in $runFiles) {
    try {
        ([IO.File]::ReadAllText($runFile.FullName, [Text.Encoding]::UTF8)) | ConvertFrom-Json
    }
    catch {
        Write-Warning "Skipping invalid benchmark file: $($runFile.FullName)"
    }
}

$runs = @($runs)
if ($runs.Count -eq 0) {
    throw 'No valid benchmark run files were found.'
}

$template = [IO.File]::ReadAllText($templatePath, [Text.Encoding]::UTF8)
$json = ConvertTo-Json -InputObject $runs -Depth 100 -Compress
$base64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))

if (-not $template.Contains('__BENCHMARK_DATA_BASE64__')) {
    throw 'The benchmark data placeholder is missing from the HTML template.'
}

$report = $template.Replace('__BENCHMARK_DATA_BASE64__', $base64)
[IO.Directory]::CreateDirectory($benchmarkRoot) | Out-Null
[IO.File]::WriteAllText($outputPath, $report, [Text.UTF8Encoding]::new($false))

Write-Host "Loaded $($runs.Count) benchmark runs."
Write-Host "Report: $outputPath"
Start-Process -FilePath $outputPath
