#ifndef AppVersion
  #define AppVersion "1.0.6"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\dist\v" + AppVersion
#endif
#define AppName "Ek Ders Hakediş Sistemi"
#define AppExeName AppName + ".exe"

[Setup]
AppId={{733BEB22-6C47-49EC-9D19-1404F74058F1}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
DisableWelcomePage=no
UsePreviousAppDir=yes
UsePreviousTasks=yes
OutputDir=..\dist\installer
OutputBaseFilename=Ek-Ders-Hakedis-Sistemi-Kurulum-{#AppVersion}
SetupIconFile=..\src\Hakedis.App\Assets\Hakedis.ico
UninstallDisplayIcon={app}\{#AppExeName}
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
VersionInfoVersion={#AppVersion}

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstünde program kısayolu oluştur"; GroupDescription: "Kısayollar:"

[Files]
; Explicit allowlist: never distribute user databases, exports, or backups.
Source: "{#PayloadDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\KULLANIM.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{#AppName} programını başlat"; Flags: nowait postinstall skipifsilent

[Messages]
WelcomeLabel2=Bu sihirbaz {#AppName} programını bilgisayarınıza kuracaktır.%n%nVeriler, Çıktılar ve Yedekler masaüstünüzdeki "Ek Ders Hakediş Sistemi" klasöründe tutulur. Güncelleme ve kaldırma işlemleri bu klasörleri silmez.%n%nDevam etmeden önce açık programda çalışmalarınızı kaydedin.
