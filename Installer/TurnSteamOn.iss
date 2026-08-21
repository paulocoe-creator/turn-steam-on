#define AppName "Turn Steam On"
#define AppVersion "1.0.0"
#define AppPublisher "Paulo Coelho"
#define AppExeName "TurnSteamOn.exe"
#define SourceRoot "..\TurnSteamOn\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"

[Setup]
AppId={{A8E0E4D4-9E1A-4B28-9D87-41DA5A0F6B7A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
SetupIconFile={#SourceRoot}\Assets\favicon.ico
OutputDir=output
OutputBaseFilename=TurnSteamOn-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64
ChangesAssociations=no

[Files]
Source: "{#SourceRoot}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Assets\favicon.ico"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

