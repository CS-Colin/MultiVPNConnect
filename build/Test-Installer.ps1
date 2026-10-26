param([string]$NsisCompiler,[string]$BuildDirectory='dist\0.6.2')
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 if(-not $NsisCompiler){$NsisCompiler=(Get-ChildItem (Join-Path $env:LOCALAPPDATA 'electron-builder\Cache') -Filter makensis.exe -Recurse | Where-Object {$_.Directory.Name -ne 'Bin'} | Select-Object -First 1).FullName}
 if(-not $NsisCompiler){throw 'Pass -NsisCompiler.'}
 $reg=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,[Microsoft.Win32.RegistryView]::Registry32)
 $appKey='Software\MultiVPNConnect.InstallerTest'
 $uninstallKey='Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiVPNConnect.InstallerTest'
 if($reg.OpenSubKey($appKey)){throw 'A previous installer test registration exists. Resolve it before running a new test.'}
 $buildPath=(Resolve-Path $BuildDirectory).Path
 $expectedVersion=([Version](Get-Item (Join-Path $buildPath 'MultiVPNConnect.exe')).VersionInfo.FileVersion).ToString(3)
 $testRoot=Join-Path (Resolve-Path dist).Path ('installer-test-'+[Guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory -Force $testRoot | Out-Null
 $target=Join-Path $testRoot 'App With Spaces'
 $setup=Join-Path $testRoot 'Test-Setup.exe'
 & $NsisCompiler /V2 /DTEST_BUILD "/DAPP_VERSION=$expectedVersion" "/DBUILD_DIR=$buildPath" "/DOUTPUT_FILE=$setup" installer\MultiVPNConnect.nsi
 if($LASTEXITCODE -ne 0){throw 'Test installer build failed'}
 function Assert($value,$message){if(-not $value){throw $message};Write-Output "PASS: $message"}
 function Install-Test {
  $p=Start-Process -FilePath $setup -ArgumentList "/S /D=$target" -PassThru -Wait -WindowStyle Hidden
  return $p.ExitCode
 }
 Assert ((Install-Test) -eq 0) 'Wizard silent installation succeeds in a path with spaces'
 Assert (Test-Path -LiteralPath "$target\MultiVPNConnect.exe") 'Application installed'
 Assert (Test-Path -LiteralPath "$target\Uninstall.exe") 'Windows uninstaller installed'
 Assert (Test-Path -LiteralPath "$target\TestShortcuts\MultiVPNConnect.lnk") 'Start menu shortcut created in isolated test location'
 Assert (Test-Path -LiteralPath "$target\TestDesktop\MultiVPNConnect.lnk") 'Desktop shortcut created in isolated test location'
 $key=$reg.OpenSubKey($uninstallKey)
 Assert ($key.GetValue('DisplayVersion') -eq $expectedVersion) 'Installed Apps version registered'
 $key.Dispose()
 $appHash=(Get-FileHash -LiteralPath "$target\MultiVPNConnect.exe").Hash
 # Simulate a running app's lock without touching any real VPN process.
 $lock=[IO.File]::Open("$target\MultiVPNConnect.exe",[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try {Assert ((Install-Test) -eq 2) 'Upgrade refuses to overwrite a locked app';Assert ((Get-FileHash -LiteralPath "$target\MultiVPNConnect.exe").Hash -eq $appHash) 'Blocked upgrade keeps the current app intact'} finally {$lock.Dispose()}
 Set-Content -LiteralPath "$target\profiles.json" -Value '{"keep":"profile-sentinel"}'
 Assert ((Install-Test) -eq 0) 'Reinstall and upgrade succeed after the app is closed'
 $alternate=Join-Path $testRoot 'Should Not Be Used'
 $p=Start-Process -FilePath $setup -ArgumentList "/S /D=$alternate" -PassThru -Wait -WindowStyle Hidden
 Assert ($p.ExitCode -eq 0 -and -not (Test-Path -LiteralPath "$alternate\MultiVPNConnect.exe")) 'Upgrade retains the registered install folder'
 Assert ((Get-Content -LiteralPath "$target\profiles.json" -Raw).Contains('profile-sentinel')) 'Upgrade preserves existing local files'
 $p=Start-Process -FilePath "$target\Uninstall.exe" -ArgumentList '/S' -PassThru -Wait -WindowStyle Hidden
 for($i=0;$i -lt 50;$i++){if(-not(Test-Path -LiteralPath "$target\MultiVPNConnect.exe")){break};Start-Sleep -Milliseconds 200}
 Assert (-not(Test-Path -LiteralPath "$target\MultiVPNConnect.exe")) 'Uninstall removes the app'
 Assert ($null -eq $reg.OpenSubKey($uninstallKey)) 'Uninstall removes Installed Apps registration'
 Assert ($null -eq $reg.OpenSubKey($appKey)) 'Uninstall removes installer registration'
 Assert (-not(Test-Path -LiteralPath "$target\TestShortcuts\MultiVPNConnect.lnk")) 'Uninstall removes its shortcut'
 Assert ((Get-Content -LiteralPath "$target\profiles.json" -Raw).Contains('profile-sentinel')) 'Uninstall preserves unrelated files and profile data'
 $reg.Dispose()
 Write-Output "Installer lifecycle passed. Test files: $testRoot"
} finally {Pop-Location}
