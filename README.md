# MultiVPNConnect 0.3.2

Version 0.3.2 loads the window icon directly from an embedded branding resource and includes nine native icon sizes for the taskbar and display scaling. Native Windows small/large window-icon checks passed.

Version 0.3 adds a redesigned dark interface with searchable connections, protocol accents, a large connection status card, clear action buttons, and an activity panel. The VPN connection adapters are unchanged from the working 0.2 build.

Windows VPN desktop app with in-app Connect, Disconnect, sign-in prompts, connection status, and session events. Uses installed VPN engines without handing you off to their separate graphical interfaces.

## Install and use
Run `dist/0.3.2/MultiVPNConnect-Setup.exe`, then launch from the Start menu. Accept Windows administrator permission so the engines can configure adapters and routes. Existing profile references from 0.1 are retained.

1. Import a WireGuard `.conf`, OpenVPN `.ovpn`, or Sophos `.scx`, `.tgb`, or `.pro` file. Keep the original file and its certificate dependencies in place.
2. Alternatively select **Use Sophos profiles** to load names of connections already imported into the installed Sophos engine.
3. Select a profile and click **Connect**. Enter credentials when requested. Click **Disconnect** to stop the session.

## Engines
- WireGuard: installs an app-owned tunnel service using the official WireGuard Windows command interface. Status distinguishes an active interface from a recent successful handshake. Configurations copied for the service have restricted filesystem permissions.
- OpenVPN: starts an installed OpenVPN executable directly and controls it through a password-protected localhost management channel. Handles username/password, private-key passwords, and static OTP challenges. The engine initiates the management connection and terminates if the app connection is lost. Relative certificates resolve against the original profile directory. Engine errors and session events appear in the app. Dynamic challenge-response, browser/SAML, PKCS#11, and external certificate flows are not implemented.
- Sophos: imports configurations into the installed Sophos engine, enables/disables named connections through SCCLI, and polls the engine's VPN state. Existing engine profiles can be selected. Username/password (including appended OTP where your server requires it) is prompted in the app; passwords are supplied through standard input, not process arguments or disk. Certificate prompts, provisioning/browser authentication, and endpoint policy interactions require further compatibility testing. A Sophos `.ovpn` SSL configuration can also use the OpenVPN adapter.

Official WireGuard, OpenVPN, and/or Sophos engine packages must be installed separately. The app detects the engines already installed on this computer, including Sophos's OpenVPN binary. Engines and drivers are not redistributed in this installer. Running as administrator is the current implementation; a dedicated privileged broker service is a future improvement. One session at a time is supported. Closing the app asks to disconnect. WireGuard/Sophos services can survive an app crash; selecting the same profile allows reconnection/control, but automatic crash recovery is not implemented. No kill switch, traffic-leak guarantees, or unattended reconnect feature is claimed.

## Build and tests
Run `powershell -ExecutionPolicy Bypass -File .\build.ps1`. Requires the Windows .NET Framework 4.x compiler. Output is `dist/0.3.2/`. This installer and app are unsigned.

`tests/Tests.cs` and `tests/FakeEngine.cs` exercise Windows argument escaping, Sophos status parsing, OpenVPN authentication/state/disconnection using a localhost simulator, cancelled-login cleanup, and desktop initialization. Compile tests with `TEST_ENGINE` and `/main:Tests`; the engine override is excluded from production builds. Pass the simulator executable and a scratch directory, optionally followed by an installed OpenVPN executable for a null-device localhost-only management startup/shutdown check.

Validation performed: compilation, isolated simulator tests, actual installed OpenVPN management startup/shutdown on a null device with only localhost traffic, and desktop initialization. No successful remote VPN tunnel or real-account authentication has been verified. WireGuard service activation and Sophos authentication require real endpoint testing.

## Installer and updates

Version 0.3.1 replaces the basic copy-and-message-box installer with an NSIS Modern UI setup wizard, matching the installation style used by Chatterbox. It includes branded welcome and finish pages, a license page, detection of installed engines, an optional desktop shortcut, an installation-folder choice on first install, progress/details, and an optional launch at completion. No drivers are installed automatically.

Installation is per-user and does not require administrator permission. Launching the VPN app still requires administrator permission. The default location is `%LOCALAPPDATA%\Programs\MultiVPNConnect`; upgrades keep the registered installation folder. Existing 0.3.0 installations in the default folder can be upgraded in place. Close/disconnect the app before upgrading. Setup checks for locked files and offers Retry/Cancel rather than forcibly stopping connections.

The app is registered in Windows Installed Apps with a proper uninstaller, version, publisher, and icon. Uninstall removes only owned application files and shortcuts; saved profiles, original VPN configurations, existing VPN engines, and unrelated files are preserved. Use Windows Settings > Apps > Installed apps > MultiVPNConnect > Uninstall.

The built-in updater continues to use the same installer asset name and SHA256SUMS.txt format. It checks this repository's latest GitHub Release, asks before installing a newer numeric version, verifies the checksum, and launches the setup wizard. Published v0.3.0 assets are unchanged by this local 0.3.1 build. Installers are currently unsigned.

Build requirements: Windows .NET Framework 4.x compiler and NSIS 3. `build.ps1` discovers the NSIS compiler in PATH, standard installation folders, or the local electron-builder cache; alternatively pass `-NsisCompiler`. The app version determines the default output directory. GitHub Actions installs NSIS and builds to `dist/release`.

`build/Test-Installer.ps1` builds a test-only package using separate registry keys and shortcut locations. It checks installation into a path with spaces, Installed Apps registration, shortcuts, locked-file upgrade refusal, reinstall, uninstall, and preservation of profile sentinel files. These lifecycle checks passed locally. Interactive wizard layout still needs a manual visual check because desktop automation was unavailable during this build.

## References
- https://git.zx2c4.com/wireguard-windows/about/docs/enterprise.md
- https://openvpn.net/community-docs/management-interface.html
- https://openvpn.net/community-docs/community-articles/openvpn-2-6-manual.html
- Installed Sophos Connect `sccli --help`, `enable --help`, `add --help`, and `list --help`.

GPL-3.0; see LICENSE.


