param([string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
if ($Runtime -notin @('win-x64', 'win-arm64')) { throw 'Desteklenmeyen yayın hedefi.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    dotnet test tests/Hakedis.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız; paket hazırlanmadı.' }
    $packagePath = Join-Path $projectRoot ('dist/Ek Ders Hakediş Sistemi-' + $Runtime + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    dotnet publish src/Hakedis.App -c Release -r $Runtime --self-contained true -o $packagePath /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'EXE yayını başarısız.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/KULLANIM.txt') -Destination (Join-Path $packagePath 'KULLANIM.txt')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts/Create-DesktopShortcut.ps1') -Destination (Join-Path $packagePath 'Masaüstü Kısayolu.ps1')
    $allowedFiles = @('Ek Ders Hakediş Sistemi.exe', 'KULLANIM.txt', 'Masaüstü Kısayolu.ps1')
    $unexpected = @(Get-ChildItem -LiteralPath $packagePath -Force | Where-Object { $_.PSIsContainer -or $_.Name -notin $allowedFiles })
    if ($unexpected.Count -gt 0) { throw 'Paket yalnız program ve kullanım dosyalarını içerebilir; beklenmeyen dosyalar nedeniyle paketleme durduruldu.' }
    Compress-Archive -Path (Join-Path $packagePath '*') -DestinationPath ($packagePath + '.zip')
    Write-Host ('Hazır: ' + $packagePath + '.zip')
} finally { Pop-Location }
