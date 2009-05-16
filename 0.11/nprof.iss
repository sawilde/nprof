; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
AppName=NProf-0.11.1
ChangesAssociations=yes
AppVerName=NProf 0.10.1
AppPublisher=NProf Community
AppPublisherURL=http://code.google.com/p/nprof/
AppSupportURL=http://code.google.com/p/nprof/
AppUpdatesURL=http://code.google.com/p/nprof/
DefaultDirName={pf}/NProf-0.11.1
DefaultGroupName=NProf-0.11.1
DisableProgramGroupPage=yes
OutputDir=Releases
OutputBaseFilename=NProf-0.11.1-setup
SetupIconFile=NProf\App.ico
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Files]
Source: NProf\bin\Release\NProf.exe; DestDir: {app}; Flags: ignoreversion
Source: NProf\bin\Release\DotNetLib.Windows.Forms.dll; DestDir: {app}; Flags: ignoreversion
Source: NProf\bin\Release\DotNetLib.Windows.Forms.Themes.dll; DestDir: {app}; Flags: ignoreversion
Source: registerHook.bat; DestDir: {app}; Flags: ignoreversion
Source: Hook\Release\NProf.Hook.dll; DestDir: {app}; Flags: ignoreversion
;Source: Libraries\DotNetLib\msvcr70.dll; DestDir: {app}; Flags: ignoreversion

[Icons]
Name: {group}\..\NProf 0.11.1; Filename: {app}\NProf.exe

[Run]
Filename: {app}\registerHook.bat;  Flags: runhidden waituntilidle
Filename: {app}\NProf.exe; Description: {cm:LaunchProgram,NProf}; Flags: nowait postinstall skipifsilent
