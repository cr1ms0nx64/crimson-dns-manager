param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration
$payload = Join-Path $PSScriptRoot "CrimsonDnsManager.App/bin/x64/$Configuration/net8.0-windows10.0.19041.0/win-x64"
$packageRoot = Join-Path $PSScriptRoot 'dist/DnsManager'
$appRoot = Join-Path $packageRoot 'app'
# Start fresh so removed dependencies cannot survive a subsequent package build.
if (Test-Path -LiteralPath $packageRoot) {
    $resolved = (Resolve-Path -LiteralPath $packageRoot).Path
    $allowed = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'dist/DnsManager'))
    if ($resolved -ne $allowed) { throw 'Unerwarteter Paketpfad.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
New-Item -ItemType Directory -Path $appRoot -Force | Out-Null
Get-ChildItem -LiteralPath $payload | Where-Object Extension -ne '.pdb' | Copy-Item -Destination $appRoot -Recurse -Force
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $csc /nologo /target:winexe /platform:x64 /reference:System.Windows.Forms.dll "/win32icon:$PSScriptRoot/CrimsonDnsManager.App/Assets/Icons/app.ico" "/out:$packageRoot/CrimsonDnsManager.App.exe" (Join-Path $PSScriptRoot 'launcher/Program.cs')
if ($LASTEXITCODE -ne 0) { throw 'Programmstarter konnte nicht erstellt werden.' }
Write-Host "Fertiges Programmpaket: $packageRoot"
