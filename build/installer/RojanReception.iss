; ROJAN Reception Setup - Desktop Productionization Sprint 1.
;
; Packages the self-contained, single-file win-x64 build already produced
; by build/publish.ps1 (publish/Rojan.Desktop.Shell.exe and its Languages/
; sidecar files) into one distributable "ROJAN Reception Setup.exe" -
; versioned, Start Menu shortcut, uninstaller, clean install/upgrade flow.
; Never run directly - build/publish-installer.ps1 invokes ISCC.exe with
; /DAppVersion=<version> (from Directory.Build.props via get-version.ps1),
; the single source of truth every other release artifact already uses.
;
; PrivilegesRequired=lowest (per-user install, no admin prompt) - matches
; this app's own DPAPI-encrypted-per-user-account session storage
; (Infrastructure.Security.DpapiSecureStorageService) and is the right
; default for a salon reception PC where the operator may not have admin
; rights. AppId is a fixed GUID (not the app name) - Inno Setup's own
; documented mechanism for reliably detecting "this is an upgrade of an
; already-installed version" across releases; it must never change.
#ifndef AppVersion
  #define AppVersion "0.1.0-alpha"
#endif

#define AppName "ROJAN Reception"
#define AppPublisher "ROJAN"
#define AppExeName "Rojan.Desktop.Shell.exe"
#define AppId "{{D804D0AC-BF41-4A54-8904-D9EC1BB773CF}"
#define PublishDir "..\..\publish"
#define ArtifactsDir "..\..\artifacts"
#define AssetsDir "..\..\src\Rojan.Desktop.Shell\Assets"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#ArtifactsDir}
OutputBaseFilename=ROJAN Reception Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Desktop Productionization Sprint 2 (Production Branding): the real
; ROJAN brand mark (see build/generate-icon.ps1) - the installer wizard's
; own icon. Shortcuts don't need a separate icon binding - [Icons] below
; already points them at {#AppExeName}, which carries this same icon
; embedded (Shell.csproj's <ApplicationIcon>), so they inherit it
; automatically.
SetupIconFile={#AssetsDir}\RojanReception.ico
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableWelcomePage=no
SetupLogging=yes
; Desktop Productionization Sprint 2 (Code Signing Preparation): both
; directives are inert unless build/publish-installer.ps1 was invoked
; with -CertificatePath, which sets the SignInstaller preprocessor symbol
; and defines the "signtool" tool these directives reference (via ISCC's
; /S switch) - see that script's own doc comment for the full mechanism.
; With no certificate (the default, and the only path this environment
; can actually exercise - see docs/standards/code-signing.md), this
; entire block is skipped and the installer builds exactly as before:
; the "unsigned fallback for development" this sprint asks to keep.
#ifdef SignInstaller
SignTool=signtool
SignedUninstaller=yes
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; The entire self-contained, single-file publish output - Rojan.Desktop.Shell.exe
; itself, its .pdb/.xml sidecars, and the Languages\*.pack folder Phase 19A's
; LanguagePackManager scans at startup (see Shell.csproj's own comment).
; recursesubdirs so Languages\ comes along without listing it separately.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Removes the app's LocalAppData settings (theme/language/API-environment
; JSON files under %LocalAppData%\RojanDesktop) on uninstall - a clean
; uninstall per the sprint's own requirement. DPAPI-encrypted session/auth
; data lives under the same root (Infrastructure.Security.DpapiSecureStorageService)
; and is intentionally included: an uninstall is a real removal, not a
; "keep credentials around for a future reinstall" scenario.
Type: filesandordirs; Name: "{userappdata}\..\Local\RojanDesktop"
