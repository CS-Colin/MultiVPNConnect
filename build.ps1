param([string]$OutputDirectory = '',[string]$NsisCompiler = '')
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
 $source = Get-Content src/App.cs -Raw
 $version = [regex]::Match($source,'const string Version="([0-9.]+)"').Groups[1].Value
 if (-not $version) { throw 'App version not found' }
 if (-not $OutputDirectory) { $OutputDirectory = "dist\$version" }
 if (-not $NsisCompiler) {
  $command = Get-Command makensis.exe -ErrorAction SilentlyContinue
  if ($command) { $NsisCompiler=$command.Source }
  else {
   foreach($candidate in @("${env:ProgramFiles(x86)}\NSIS\makensis.exe", "$env:ProgramFiles\NSIS\makensis.exe")) { if (Test-Path -LiteralPath $candidate) { $NsisCompiler=$candidate;break } }
   if (-not $NsisCompiler) {
    $cache=Join-Path $env:LOCALAPPDATA 'electron-builder\Cache'
    if (Test-Path -LiteralPath $cache) { $found=Get-ChildItem -LiteralPath $cache -Filter makensis.exe -Recurse | Where-Object { $_.Directory.Name -ne 'Bin' } | Select-Object -First 1; if($found){$NsisCompiler=$found.FullName} }
   }
  }
 }
 if (-not $NsisCompiler -or -not (Test-Path -LiteralPath $NsisCompiler)) { throw 'Install NSIS 3 or pass -NsisCompiler with the path to makensis.exe.' }
 New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
 $outPath=(Resolve-Path $OutputDirectory).Path
 $app=Join-Path $outPath 'MultiVPNConnect.exe'
 $setup=Join-Path $outPath 'MultiVPNConnect-Setup.exe'
 & $compiler /nologo /target:winexe "/out:$app" /win32manifest:src\app.manifest /win32icon:installer\assets\app.ico /resource:installer\assets\app.ico,MultiVPNConnect.AppIcon /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.ServiceProcess.dll /reference:System.Net.Http.dll src\App.cs src\Engines.cs src\Theme.cs src\Branding.cs src\Details.cs src\Desktop.cs src\InternetIdentity.cs src\NetworkFacts.cs src\NetworkFields.cs src\Credentials.cs src\LoginDialog.cs src\UpdateRelease.cs
 if ($LASTEXITCODE -ne 0) { throw 'App compilation failed' }
 & $compiler /nologo /target:exe "/out:$outPath\InstallerGuard.exe" installer\InstallerGuard.cs
 if ($LASTEXITCODE -ne 0) { throw 'Installer guard compilation failed' }
 & $NsisCompiler /V2 "/DAPP_VERSION=$version" "/DBUILD_DIR=$outPath" "/DOUTPUT_FILE=$setup" installer\MultiVPNConnect.nsi
 if ($LASTEXITCODE -ne 0) { throw 'Wizard installer compilation failed' }
 $hash=(Get-FileHash $setup -Algorithm SHA256).Hash.ToLowerInvariant()
 "$hash  MultiVPNConnect-Setup.exe" | Set-Content (Join-Path $outPath 'SHA256SUMS.txt') -Encoding ASCII
 Write-Output "Built MultiVPNConnect $version in $outPath"
} finally { Pop-Location }
