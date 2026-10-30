param(
    [string]$Version = '1.0.6',
    [string]$CompilerPath
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw 'Version must contain three or four numeric components.'
}
$payloadDir = Join-Path $repoRoot "dist\v$Version"
$requiredFiles = @('Ek Ders Hakediş Sistemi.exe', 'KULLANIM.txt')
foreach ($name in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $payloadDir $name) -PathType Leaf)) {
        throw "Publish the application first. Missing: $payloadDir\$name"
    }
}
# Reject unexpected contents so personal data cannot accidentally enter a release.
$unexpected = @(Get-ChildItem -LiteralPath $payloadDir -Force | Where-Object { $_.Name -notin $requiredFiles -or $_.PSIsContainer })
if ($unexpected.Count -gt 0) {
    throw "Payload must contain only the executable and KULLANIM.txt. Unexpected: $($unexpected.Name -join ', ')"
}
if (-not $CompilerPath) {
    $candidates = @(
        (Join-Path $repoRoot 'artifacts\tools\InnoSetup\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) {
    throw 'Install official Inno Setup 6 or pass -CompilerPath with the path to ISCC.exe.'
}
& $CompilerPath "/DAppVersion=$Version" "/DPayloadDir=$payloadDir" (Join-Path $repoRoot 'installer\Hakedis.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed: $LASTEXITCODE" }
$installerPath = Join-Path $repoRoot "dist\installer\Ek-Ders-Hakedis-Sistemi-Kurulum-$Version.exe"
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) { throw "Installer was not generated: $installerPath" }
Get-Item -LiteralPath $installerPath
Get-FileHash -LiteralPath $installerPath -Algorithm SHA256
