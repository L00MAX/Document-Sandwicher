#ifndef AppVersion
  #define AppVersion "1.4.0"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\dist\installer-payload"
#endif

[Setup]
AppId={{76088FC9-191C-47ED-BF87-320E2E2B2C04}
AppName=Document Sandwich
AppVersion={#AppVersion}
AppVerName=Document Sandwich {#AppVersion}
DefaultDirName=C:\Program Files\Document Sandwich
DisableDirPage=yes
UsePreviousAppDir=yes
DefaultGroupName=Document Sandwich
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\dist
OutputBaseFilename=Document-Sandwich-Setup-{#AppVersion}
SetupIconFile=..\assets\sandwich.ico
UninstallDisplayIcon={app}\Document Sandwich.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Files]
Source: "{#PayloadDir}\Document Sandwich.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; DestName: "Readme.txt"; Flags: ignoreversion
Source: "..\assets\sandwich.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "licenses\*"; DestDir: "{app}\Licenses"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: files; Name: "{app}\PDF Sandwich.exe"
Type: files; Name: "{app}\PDF Sandwich.pdb"
Type: files; Name: "{commondesktop}\PDF Sandwich.lnk"
Type: files; Name: "{commonprograms}\PDF Sandwich.lnk"

[Icons]
Name: "{commondesktop}\Document Sandwich"; Filename: "{app}\Document Sandwich.exe"; WorkingDir: "{app}"; IconFilename: "{app}\sandwich.ico"
Name: "{commonprograms}\Document Sandwich"; Filename: "{app}\Document Sandwich.exe"; WorkingDir: "{app}"; IconFilename: "{app}\sandwich.ico"

[Run]
Filename: "{app}\Document Sandwich.exe"; Description: "Open Document Sandwich"; Flags: nowait postinstall skipifsilent unchecked
