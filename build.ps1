$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
dotnet restore .\CrimsonDnsManager.sln
dotnet build .\CrimsonDnsManager.sln -c Release -p:Platform=x64
