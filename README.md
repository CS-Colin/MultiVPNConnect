# MultiVPNConnect 0.3.0

Version 0.3 adds a redesigned dark interface with searchable connections, protocol accents, a large connection status card, clear action buttons, and an activity panel. The VPN connection adapters are unchanged from the working 0.2 build.

Windows VPN desktop app with in-app Connect, Disconnect, sign-in prompts, connection status, and session events. Uses installed VPN engines without handing you off to their separate graphical interfaces.

## Install and use
Run `dist/0.3.0/MultiVPNConnect-Setup.exe`, then launch from the Start menu. Accept Windows administrator permission so the engines can configure adapters and routes. Existing profile references from 0.1 are retained.

1. Import a WireGuard `.conf`, OpenVPN `.ovpn`, or Sophos `.scx`, `.tgb`, or `.pro` file. Keep the original file and its certificate dependencies in place.
2. Alternatively select **Use Sophos profiles** to load names of connections already imported into the installed Sophos engine.
3. Select a profile and click **Connect**. Enter credentials when requested. Click **Disconnect** to stop the session.

## Engines
- WireGuard: installs an app-owned tunnel service using the official WireGuard Windows command interface. Status distinguishes an active interface from a recent successful handshake. Configurations copied for the service have restricted filesystem permissions.
- OpenVPN: starts an installed OpenVPN executable directly and controls it through a password-protected localhost management channel. Handles username/password, private-key passwords, and static OTP challenges. The engine initiates the management connection and terminates if the app connection is lost. Relative certificates resolve against the original profile directory. Engine errors and session events appear in the app. Dynamic challenge-response, browser/SAML, PKCS#11, and external certificate flows are not implemented.
- Sophos: imports configurations into the installed Sophos engine, enables/disables named connections through SCCLI, and polls the engine's VPN state. Existing engine profiles can be selected. Username/password (including appended OTP where your server requires it) is prompted in the app; passwords are supplied through standard input, not process arguments or disk. Certificate prompts, provisioning/browser authentication, and endpoint policy interactions require further compatibility testing. A Sophos `.ovpn` SSL configuration can also use the OpenVPN adapter.

Official WireGuard, OpenVPN, and/or Sophos engine packages must be installed separately. The app detects the engines already installed on this computer, including Sophos's OpenVPN binary. Engines and drivers are not redistributed in this installer. Running as administrator is the current implementation; a dedicated privileged broker service is a future improvement. One session at a time is supported. Closing the app asks to disconnect. WireGuard/Sophos services can survive an app crash; selecting the same profile allows reconnection/control, but automatic crash recovery is not implemented. No kill switch, traffic-leak guarantees, or unattended reconnect feature is claimed.

## Build and tests
Run `powershell -ExecutionPolicy Bypass -File .\build.ps1`. Requires the Windows .NET Framework 4.x compiler. Output is `dist/0.3.0/`. This installer and app are unsigned.

`tests/Tests.cs` and `tests/FakeEngine.cs` exercise Windows argument escaping, Sophos status parsing, OpenVPN authentication/state/disconnection using a localhost simulator, cancelled-login cleanup, and desktop initialization. Compile tests with `TEST_ENGINE` and `/main:Tests`; the engine override is excluded from production builds. Pass the simulator executable and a scratch directory, optionally followed by an installed OpenVPN executable for a null-device localhost-only management startup/shutdown check.

Validation performed: compilation, isolated simulator tests, actual installed OpenVPN management startup/shutdown on a null device with only localhost traffic, and desktop initialization. No successful remote VPN tunnel or real-account authentication has been verified. WireGuard service activation and Sophos authentication require real endpoint testing.

## Installer and updates
Per-user installation under `%LOCALAPPDATA%\Programs\MultiVPNConnect` with a Start menu shortcut. Profile references are in `%LOCALAPPDATA%\MultiVPNConnect`. Setup requests administrator permission to close upgraded elevated copies of the app. Disconnect before updating. Existing app versions are replaced only after staging the new executable.

The built-in updater checks this repository's latest GitHub Release, asks before installing a newer numeric version, downloads the installer and SHA256SUMS.txt, verifies the checksum, then launches Setup. Checksums detect corruption, not publisher-account compromise. Published installers are available on the repository Releases page.

The workflow builds on version tags and creates a draft GitHub Release. Keep the source version, build output directory, workflow paths, and release tag in sync. Publish both MultiVPNConnect-Setup.exe and SHA256SUMS.txt. Production distribution should add trusted code signing and bundled/automated engine provisioning with the applicable licenses.

To uninstall, close/disconnect the app, delete its installation directory and Start menu shortcut. Optionally remove saved app data. Configurations imported into Sophos remain in that engine; remove them there if no longer needed.

## References
- https://git.zx2c4.com/wireguard-windows/about/docs/enterprise.md
- https://openvpn.net/community-docs/management-interface.html
- https://openvpn.net/community-docs/community-articles/openvpn-2-6-manual.html
- Installed Sophos Connect `sccli --help`, `enable --help`, `add --help`, and `list --help`.

GPL-3.0; see LICENSE.
