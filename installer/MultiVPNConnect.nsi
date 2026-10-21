# -*- coding: utf-8 -*-
Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "nsDialogs.nsh"
!include "Sections.nsh"
!include "x64.nsh"
!ifndef APP_VERSION
 !define APP_VERSION "0.5.0"
!endif
!ifndef BUILD_DIR
 !error "Pass BUILD_DIR pointing to compiled app files."
!endif
!ifndef OUTPUT_FILE
 !error "Pass OUTPUT_FILE pointing to the setup executable."
!endif
!ifdef TEST_BUILD
 !define REGKEY "Software\MultiVPNConnect.InstallerTest"
 !define UNINSTALLKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiVPNConnect.InstallerTest"
!else
 !define REGKEY "Software\MultiVPNConnect"
 !define UNINSTALLKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiVPNConnect"
!endif
!define PRODUCT "MultiVPNConnect"
Name "${PRODUCT} ${APP_VERSION}"
OutFile "${OUTPUT_FILE}"
InstallDir "$LOCALAPPDATA\Programs\MultiVPNConnect"
InstallDirRegKey HKCU "${REGKEY}" "InstallLocation"
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetOverwrite on
BrandingText "MultiVPNConnect  |  Your network. Your connection."
ShowInstDetails show
ShowUninstDetails show
VIProductVersion "${APP_VERSION}.0"
VIAddVersionKey "ProductName" "MultiVPNConnect Setup"
VIAddVersionKey "FileDescription" "MultiVPNConnect Setup Wizard"
VIAddVersionKey "CompanyName" "CS-Colin"
VIAddVersionKey "LegalCopyright" "GPL-3.0"
VIAddVersionKey "FileVersion" "${APP_VERSION}"
!define MUI_ICON "assets\app.ico"
!define MUI_UNICON "assets\app.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP "assets\welcome.bmp"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP "assets\header.bmp"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "Welcome to MultiVPNConnect"
!define MUI_WELCOMEPAGE_TEXT "One app for your VPN connections.$\r$\n$\r$\nThis wizard will install MultiVPNConnect ${APP_VERSION} for your Windows account.$\r$\n$\r$\nDisconnect and close any running copy before upgrading. Your saved profiles and original VPN configurations will be kept."
!define MUI_FINISHPAGE_TITLE "You're ready to connect"
!define MUI_FINISHPAGE_TEXT "MultiVPNConnect is installed.$\r$\n$\r$\nOpen it from the Start menu, choose a connection, and click Connect.$\r$\n$\r$\nWindows will ask for administrator permission when the app starts so it can manage VPN adapters."
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "Launch MultiVPNConnect"
!define MUI_FINISHPAGE_RUN_FUNCTION LaunchApp
!define MUI_UNCONFIRMPAGE_TEXT_TOP "Remove MultiVPNConnect from this computer? Saved profiles and original VPN configurations will be kept. Disconnect and close the app before continuing."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
Page custom EnginesPage
!insertmacro MUI_PAGE_COMPONENTS
!define MUI_PAGE_CUSTOMFUNCTION_PRE DirectoryPagePre
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "English"
Var EngineDialog
Var EngineText
Var StartFolder
Var DesktopFolder
Var PreviousInstall

Function .onInit
 SetShellVarContext current
 ReadRegStr $PreviousInstall HKCU "${REGKEY}" "InstallLocation"
 ${If} $PreviousInstall != ""
  StrCpy $INSTDIR $PreviousInstall
 ${EndIf}
 !ifdef TEST_BUILD
  StrCpy $StartFolder "$INSTDIR\TestShortcuts"
  StrCpy $DesktopFolder "$INSTDIR\TestDesktop"
 !else
  StrCpy $StartFolder "$SMPROGRAMS\MultiVPNConnect"
  StrCpy $DesktopFolder "$DESKTOP"
 !endif
FunctionEnd

Function DirectoryPagePre
 ; Upgrades retain the registered folder to avoid leaving duplicate installations.
 ${If} $PreviousInstall != ""
  Abort
 ${EndIf}
FunctionEnd
Function LaunchApp
 ExecShell "open" "$INSTDIR\MultiVPNConnect.exe"
FunctionEnd

Function EnginesPage
 !insertmacro MUI_HEADER_TEXT "VPN engines" "See which connection engines are available on this computer."
 nsDialogs::Create 1018
 Pop $EngineDialog
 ${If} $EngineDialog == error
  Abort
 ${EndIf}
 ${NSD_CreateLabel} 0 0 100% 25u "MultiVPNConnect controls installed VPN engines directly. Install the matching engine for the profiles you use."
 Pop $0
 StrCpy $EngineText "WireGuard: not detected"
 IfFileExists "$PROGRAMFILES64\WireGuard\wireguard.exe" 0 +2
  StrCpy $EngineText "WireGuard: installed"
 ${NSD_CreateLabel} 0 36u 100% 15u "$EngineText"
 Pop $0
 StrCpy $EngineText "OpenVPN: not detected"
 IfFileExists "$PROGRAMFILES64\OpenVPN\bin\openvpn.exe" openvpn_found
 IfFileExists "$PROGRAMFILES32\OpenVPN\bin\openvpn.exe" openvpn_found
 IfFileExists "$PROGRAMFILES32\Sophos\Connect\openvpn.exe" openvpn_found
 IfFileExists "$PROGRAMFILES64\Sophos\Connect\openvpn.exe" openvpn_found openvpn_done
 openvpn_found:
  StrCpy $EngineText "OpenVPN: installed"
 openvpn_done:
 ${NSD_CreateLabel} 0 62u 100% 15u "$EngineText"
 Pop $0
 StrCpy $EngineText "Sophos Connect: not detected"
 IfFileExists "$PROGRAMFILES32\Sophos\Connect\sccli.exe" sophos_found
 IfFileExists "$PROGRAMFILES64\Sophos\Connect\sccli.exe" sophos_found sophos_done
 sophos_found:
  StrCpy $EngineText "Sophos Connect: installed"
 sophos_done:
 ${NSD_CreateLabel} 0 88u 100% 15u "$EngineText"
 Pop $0
 ${NSD_CreateLabel} 0 125u 100% 45u "You can continue installing now and add engines later. This installer does not download drivers or change your active VPN connections. Get Sophos Connect from your VPN administrator."
 Pop $0
 nsDialogs::Show
FunctionEnd

!macro GuardFiles PREFIX
 InitPluginsDir
 File /oname=$PLUGINSDIR\InstallerGuard.exe "${BUILD_DIR}\InstallerGuard.exe"
 ${PREFIX}retry_guard:
 nsExec::ExecToStack '"$PLUGINSDIR\InstallerGuard.exe" "$INSTDIR"'
 Pop $0
 Pop $1
 ${If} $0 != 0
  IfSilent ${PREFIX}guard_abort
  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "MultiVPNConnect is still running, or the installation folder is locked.$\r$\n$\r$\nDisconnect your VPN and close MultiVPNConnect, then choose Retry. No running connections will be forcibly stopped." IDRETRY ${PREFIX}retry_guard
  ${PREFIX}guard_abort:
  SetErrorLevel 2
  Abort
 ${EndIf}
!macroend

Section "MultiVPNConnect (required)" AppSection
 SectionIn RO
 !ifdef TEST_BUILD
  StrCpy $StartFolder "$INSTDIR\TestShortcuts"
  StrCpy $DesktopFolder "$INSTDIR\TestDesktop"
 !endif
 !insertmacro GuardFiles "install_"
 SetOutPath "$INSTDIR"
 ClearErrors
 File /oname=MultiVPNConnect.exe "${BUILD_DIR}\MultiVPNConnect.exe"
 IfErrors install_failed
 File /oname=LICENSE.txt "..\LICENSE"
 FileOpen $0 "$INSTDIR\install.id" w
 FileWrite $0 "MultiVPNConnect.Installer.v1"
 FileClose $0
 WriteUninstaller "$INSTDIR\Uninstall.exe"
 CreateDirectory "$StartFolder"
 CreateShortCut "$StartFolder\MultiVPNConnect.lnk" "$INSTDIR\MultiVPNConnect.exe"
 !ifndef TEST_BUILD
  Delete "$SMPROGRAMS\MultiVPNConnect.lnk"
 !endif
 WriteRegStr HKCU "${REGKEY}" "InstallLocation" "$INSTDIR"
 WriteRegStr HKCU "${UNINSTALLKEY}" "DisplayName" "MultiVPNConnect"
 WriteRegStr HKCU "${UNINSTALLKEY}" "DisplayVersion" "${APP_VERSION}"
 WriteRegStr HKCU "${UNINSTALLKEY}" "Publisher" "CS-Colin"
 WriteRegStr HKCU "${UNINSTALLKEY}" "InstallLocation" "$INSTDIR"
 WriteRegStr HKCU "${UNINSTALLKEY}" "DisplayIcon" "$INSTDIR\MultiVPNConnect.exe"
 WriteRegStr HKCU "${UNINSTALLKEY}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
 WriteRegStr HKCU "${UNINSTALLKEY}" "QuietUninstallString" '$\"$INSTDIR\Uninstall.exe$\" /S'
 WriteRegStr HKCU "${UNINSTALLKEY}" "URLInfoAbout" "https://github.com/CS-Colin/MultiVPNConnect"
 WriteRegDWORD HKCU "${UNINSTALLKEY}" "NoModify" 1
 WriteRegDWORD HKCU "${UNINSTALLKEY}" "NoRepair" 1
 WriteRegDWORD HKCU "${UNINSTALLKEY}" "EstimatedSize" 400
 Goto install_done
 install_failed:
  MessageBox MB_OK|MB_ICONSTOP "The app could not be installed. Check folder permissions and close the running app, then try again." /SD IDOK
  SetErrorLevel 2
  Abort
 install_done:
SectionEnd

Section "Desktop shortcut" DesktopSection
 CreateDirectory "$DesktopFolder"
 CreateShortCut "$DesktopFolder\MultiVPNConnect.lnk" "$INSTDIR\MultiVPNConnect.exe"
 WriteRegDWORD HKCU "${REGKEY}" "DesktopShortcut" 1
SectionEnd

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
 !insertmacro MUI_DESCRIPTION_TEXT ${AppSection} "The desktop app, Start menu shortcut, and Windows uninstaller. Existing profiles are preserved."
 !insertmacro MUI_DESCRIPTION_TEXT ${DesktopSection} "Add a MultiVPNConnect shortcut to your desktop."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section "Uninstall"
 SetShellVarContext current
 !ifdef TEST_BUILD
  StrCpy $StartFolder "$INSTDIR\TestShortcuts"
  StrCpy $DesktopFolder "$INSTDIR\TestDesktop"
 !else
  StrCpy $StartFolder "$SMPROGRAMS\MultiVPNConnect"
  StrCpy $DesktopFolder "$DESKTOP"
 !endif
 FileOpen $0 "$INSTDIR\install.id" r
 FileRead $0 $1
 FileClose $0
 ${If} $1 != "MultiVPNConnect.Installer.v1"
  MessageBox MB_OK|MB_ICONSTOP "The installation marker is missing. No files were removed. Reinstall MultiVPNConnect in this folder and try again." /SD IDOK
  SetErrorLevel 2
  Abort
 ${EndIf}
 !insertmacro GuardFiles "uninstall_"
 Delete "$INSTDIR\MultiVPNConnect.exe"
 IfFileExists "$INSTDIR\MultiVPNConnect.exe" uninstall_failed
 Delete "$INSTDIR\LICENSE.txt"
 Delete "$INSTDIR\install.id"
 Delete "$StartFolder\MultiVPNConnect.lnk"
 RMDir "$StartFolder"
 ReadRegDWORD $0 HKCU "${REGKEY}" "DesktopShortcut"
 ${If} $0 == 1
  Delete "$DesktopFolder\MultiVPNConnect.lnk"
 ${EndIf}
 !ifdef TEST_BUILD
  RMDir "$DesktopFolder"
 !endif
 DeleteRegKey HKCU "${UNINSTALLKEY}"
 DeleteRegKey HKCU "${REGKEY}"
 Delete "$INSTDIR\Uninstall.exe"
 RMDir "$INSTDIR"
 SetErrorLevel 0
 Goto uninstall_done
 uninstall_failed:
  MessageBox MB_OK|MB_ICONSTOP "The app is still locked. Close MultiVPNConnect and run the uninstaller again." /SD IDOK
  SetErrorLevel 2
  Abort
 uninstall_done:
SectionEnd
