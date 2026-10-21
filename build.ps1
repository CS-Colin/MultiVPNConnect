param([string]$OutputDirectory = 'dist\0.3.0')
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
 New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
 $app = Join-Path $OutputDirectory 'MultiVPNConnect.exe'
 $setup = Join-Path $OutputDirectory 'MultiVPNConnect-Setup.exe'
 & $compiler /nologo /target:winexe "/out:$app" /win32manifest:src\app.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.ServiceProcess.dll src\App.cs src\Engines.cs src\Theme.cs
 if ($LASTEXITCODE -ne 0) { throw 'App compilation failed' }
 & $compiler /nologo /target:winexe "/out:$setup" /win32manifest:src\app.manifest /reference:System.Windows.Forms.dll /reference:Microsoft.CSharp.dll "/resource:$app,app" src\Setup.cs
 if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
 $hash = (Get-FileHash $setup -Algorithm SHA256).Hash.ToLowerInvariant()
 "$hash  MultiVPNConnect-Setup.exe" | Set-Content (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Encoding ASCII
} finally { Pop-Location }


