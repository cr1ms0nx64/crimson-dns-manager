$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -Configuration Debug
$app = Join-Path $PSScriptRoot 'CrimsonDnsManager.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/CrimsonDnsManager.App.exe'
if (-not (Test-Path -LiteralPath $app)) { throw 'Die gebaute Anwendung wurde nicht gefunden.' }
Start-Process -FilePath $app -WorkingDirectory (Split-Path -Parent $app)
