# MultiVPNConnect 0.6.1

Version 0.3.2 loads the window icon directly from an embedded branding resource and includes nine native icon sizes for the taskbar and display scaling. Native Windows small/large window-icon checks passed.

Version 0.3 adds a redesigned dark interface with searchable connections, protocol accents, a large connection status card, clear action buttons, and an activity panel. The VPN connection adapters are unchanged from the working 0.2 build.

Windows VPN desktop app with in-app Connect, Disconnect, sign-in prompts, connection status, and session events. Uses installed VPN engines without handing you off to their separate graphical interfaces.

## Install and use
Run `dist/0.6.1/MultiVPNConnect-Setup.exe`, then launch from the Start menu. Accept Windows administrator permission so the engines can configure adapters and routes. Existing profile references from 0.1 are retained.

1. Import a WireGuard `.conf`, OpenVPN `.ovpn`, or Sophos `.scx`, `.tgb`, or `.pro` file. Keep the original file and its certificate dependencies in place.
2. Alternatively select **Use Sophos profiles** to load names of connections already imported into the installed Sophos engine.
3. Select a profile and click **Connect**. Enter credentials when requested. Click **Disconnect** to stop the session.

## Tray and connection details (0.4.0)

Minimise the window to keep the app and current session running in the system tray. Double-click the shield or choose **Open MultiVPNConnect** to restore it. Right-click the tray icon to see status, connect the selected profile, disconnect, or exit. Exit and the window close button retain the existing disconnect-and-close confirmation for a managed session. Relaunching the app restores an existing hidden instance. Tray icons and timers are disposed on exit.

The details strip shows connected time observed during the current managed session, the assigned VPN address when available, and engine-reported received/sent byte totals. The duration pauses during reconnecting and clears when the managed session ends. The public Internet egress address is checked using https://api64.ipify.org over HTTPS on startup, after a session-state change, on manual Refresh IP, and once a minute. The request follows the current system route/proxy, so split-tunnel connections may show the ISP address rather than the VPN gateway. A fresh HTTP client avoids reusing a socket from the previous route. Requests time out after eight seconds; failures show Unavailable. Previous in-flight results are discarded after session changes. The PC hostname is read locally and is never included in lookup requests. Hostname means this computer's name, not reverse DNS for the public IP. Unavailable fields are labeled explicitly rather than displayed as zero.

OpenVPN reports the assigned address and counters through its management interface. WireGuard uses its matching tunnel interface and peer transfer counters. Sophos supplies network counters through SCCLI; its `Local IP` may be the outer transport address, so the app only labels an explicitly identified virtual/tunnel address as the VPN address. Some Sophos versions will therefore show the VPN address as unavailable. Engine counters may include traffic before this app adopted an existing tunnel and are not lifetime account totals.

Validated: metric parsing and formatting, duration tracking, stale-address clearing, tray registration, minimise/restore while retaining the session, tray action availability, shared Disconnect action, Exit cleanup, existing simulator authentication/disconnection, a real OpenVPN localhost management check, and installer lifecycle tests. GUI layouts were rendered and inspected at normal and minimum window sizes. No new real remote tunnel was activated during these checks.

## Network details (0.5.0)

The Connection details tab includes live download/upload rates calculated from engine byte-counter deltas, configured server hostname and active address (hover for full values), ICMP server latency, matched-adapter DNS servers, and per-family tunnel routes. The Activity tab keeps the log accessible without crowding the details panel.

Server facts refresh at most once every ten seconds while the tunnel is active. Ping is sent only to an engine-reported IP address; no reply is labeled distinctly and does not mean the VPN has failed. DNS values are the matched adapter settings, not a guarantee about Windows NRPT/DoH policy. Full/Split labels describe routes present on the VPN interface, including paired /1 default halves, separately for IPv4 and IPv6. They do not prove effective route precedence or rule out traffic leaks. Missing adapter identity or engine fields are reported as Unavailable. Multi-peer WireGuard displays the first reported peer endpoint; hover shows both the configured host and active address.

Speed/reset parsing, route-table decoding, routing classification, endpoint parsing, existing network/tray controls, and installer lifecycle checks passed. No remote tunnel was newly activated during these checks. The existing v0.3.0/v0.3.2 source tags are retained; historical binary releases without retained source snapshots are labeled as archives in their release notes.

## Saved login (0.6.0)

Check **Remember login on this Windows account** in an OpenVPN or Sophos username/password prompt. The app stores that profile's username and base password in Windows Credential Manager (a generic credential persisted on this computer). Later prompts prefill the saved login; you can edit it before continuing. Passwords remain masked unless Show password is selected. Passwords are not added to profiles.json, configuration files, logs, or process command-line arguments by this feature.

For Sophos, enter the base password in Password and any temporary code in the separate One-time code field. Only the base password is saved; the code is appended for that authentication attempt. OpenVPN static-challenge codes and private-key prompts cannot be saved by this feature. Do not paste a combined password+temporary code into the base Password field if you want to remember only the password.

Use **Forget saved login** for the selected profile, or uncheck Remember and continue, to delete its stored username/password. Removing a profile also removes its saved login. Cancelling sign-in does not change saved credentials. A password entered with Remember is saved when Continue is selected, even if the VPN later rejects authentication; correct or forget it on the next prompt.

Saved logins belong to the Windows account running MultiVPNConnect. Using a different account at the administrator prompt uses that account's credential store. Uninstall preserves saved logins and profile data; forget logins before uninstalling or remove the app's generic entries from Windows Credential Manager afterwards.

Verified using isolated dummy credentials: Windows write/read/update/delete, Unicode fields, profile separation, prefill and masking, cancellation, OTP separation, no persistence from code-only prompts, existing tray/connection controls, and installer lifecycle tests. No real VPN password was read or stored during testing. The updated sign-in and minimum-window layouts were rendered and inspected.

## Updater fix (0.6.1)

Fixes the GitHub asset-list parser: JavaScriptSerializer can return ArrayList for nested release assets, which older versions incorrectly cast to object[]. The updater now processes enumerable asset collections, validates asset dictionaries and exact download URLs, rejects duplicate/missing assets, and matches the checksum by exact installer filename.

Versions affected by the cast error require one manual installer update to 0.6.1 or newer. The old updater cannot download its own fix. Regression tests reproduce the original ArrayList failure and verify the published GitHub response, malformed/missing/duplicate assets, unexpected download origins, and checksum filename handling. A published installer was downloaded and its checksum verified without launching it; installer lifecycle checks passed separately.

## Engines
- WireGuard: installs an app-owned tunnel service using the official WireGuard Windows command interface. Status distinguishes an active interface from a recent successful handshake. Configurations copied for the service have restricted filesystem permissions.
- OpenVPN: starts an installed OpenVPN executable directly and controls it through a password-protected localhost management channel. Handles username/password, private-key passwords, and static OTP challenges. The engine initiates the management connection and terminates if the app connection is lost. Relative certificates resolve against the original profile directory. Engine errors and session events appear in the app. Dynamic challenge-response, browser/SAML, PKCS#11, and external certificate flows are not implemented.
- Sophos: imports configurations into the installed Sophos engine, enables/disables named connections through SCCLI, and polls the engine's VPN state. Existing engine profiles can be selected. Username/password (including appended OTP where your server requires it) is prompted in the app; passwords are supplied through standard input, not process arguments or disk. Certificate prompts, provisioning/browser authentication, and endpoint policy interactions require further compatibility testing. A Sophos `.ovpn` SSL configuration can also use the OpenVPN adapter.

Official WireGuard, OpenVPN, and/or Sophos engine packages must be installed separately. The app detects the engines already installed on this computer, including Sophos's OpenVPN binary. Engines and drivers are not redistributed in this installer. Running as administrator is the current implementation; a dedicated privileged broker service is a future improvement. One session at a time is supported. Closing the app asks to disconnect. WireGuard/Sophos services can survive an app crash; selecting the same profile allows reconnection/control, but automatic crash recovery is not implemented. No kill switch, traffic-leak guarantees, or unattended reconnect feature is claimed.

## Build and tests
Run `powershell -ExecutionPolicy Bypass -File .\build.ps1`. Requires the Windows .NET Framework 4.x compiler. Output is `dist/0.6.1/`. This installer and app are unsigned.

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
