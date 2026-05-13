$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
dotnet restore .\CrimsonDnsManager.sln
dotnet run --project .\CrimsonDnsManager.App\CrimsonDnsManager.App.csproj -p:Platform=x64
