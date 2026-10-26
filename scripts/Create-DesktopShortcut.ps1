param([string]$ApplicationDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$applicationName = 'Ek Ders Hakediş Sistemi'
$desktopDirectory = [Environment]::GetFolderPath('DesktopDirectory')
$targetDirectory = Join-Path $desktopDirectory $applicationName
$sourceExecutable = Join-Path ([IO.Path]::GetFullPath($ApplicationDirectory)) ($applicationName + '.exe')
$targetExecutable = Join-Path $targetDirectory ($applicationName + '.exe')
if (-not (Test-Path -LiteralPath $sourceExecutable)) { throw 'Önce ZIP dosyasını tamamen çıkarın ve betiği EXE ile aynı klasörden çalıştırın.' }
New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
if ($sourceExecutable -ne $targetExecutable) {
    $running = Get-Process -Name $applicationName -ErrorAction SilentlyContinue
    if ($running) { throw 'Güncellemeden önce Ek Ders Hakediş Sistemi penceresini kaydedip kapatın.' }
    # Let the application migrate beside its original EXE before changing the shortcut target.
    $prepared = Start-Process -FilePath $sourceExecutable -ArgumentList '--prepare-workspace' -Wait -PassThru -WindowStyle Hidden
    if ($prepared.ExitCode -ne 0) { throw 'Eski konumdaki kayıtlar otomatik birleştirilemedi. Mevcut dosyalar ve eski kopya Yedekler altında korunuyor; Günlükler klasöründeki açıklamayı kontrol edin.' }
    Copy-Item -LiteralPath $sourceExecutable -Destination $targetExecutable -Force
    $sourceHelp = Join-Path $ApplicationDirectory 'KULLANIM.txt'
    if (Test-Path -LiteralPath $sourceHelp) { Copy-Item -LiteralPath $sourceHelp -Destination $targetDirectory -Force }
}
foreach ($folder in @('Veriler','Çıktılar','Yedekler','Günlükler','Taslaklar','Paketler')) {
    New-Item -ItemType Directory -Path (Join-Path $targetDirectory $folder) -Force | Out-Null
}
$shortcutFactory = New-Object -ComObject WScript.Shell
$shortcut = $shortcutFactory.CreateShortcut((Join-Path $desktopDirectory ($applicationName + '.lnk')))
$shortcut.TargetPath = $targetExecutable
$shortcut.WorkingDirectory = $targetDirectory
$shortcut.IconLocation = $targetExecutable + ',0'
$shortcut.Description = 'Ek Ders Hakediş Sistemi — kayıtlı hakedişler ve ödeme belgeleri'
$shortcut.Save()
Write-Output ('Masaüstü simgesi hazır: ' + $targetExecutable)
