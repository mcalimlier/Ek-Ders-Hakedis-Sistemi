param([string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
if ($Runtime -notin @('win-x64', 'win-arm64')) { throw 'Desteklenmeyen yayın hedefi.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    dotnet test tests/Hakedis.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız; paket hazırlanmadı.' }
    $packagePath = Join-Path $projectRoot ('dist/EDHS-' + $Runtime + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    dotnet publish src/Hakedis.App -c Release -r $Runtime --self-contained true -o $packagePath /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'EXE yayını başarısız.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/KULLANIM.txt') -Destination (Join-Path $packagePath 'KULLANIM.txt')
    Compress-Archive -Path (Join-Path $packagePath '*') -DestinationPath ($packagePath + '.zip')
    Write-Host ('Hazır: ' + $packagePath + '.zip')
} finally { Pop-Location }
