# Crimson DNS Manager

Native Windows Desktop App in C# with .NET 8, WinUI 3, MVVM, SQLite, Entity Framework Core and DPAPI secret storage.

## Visual source

The bundled reference file is located at:

CrimsonDnsManager.App/Reference/crimson-dns-manager_2.html

The WinUI shell, colors, spacing, typography resources, sidebar, topbar, buttons, inputs, badges, DNS table, stat pills, modal styling and livefeed are mapped from that reference.

## Project structure

CrimsonDnsManager.App

Assets
Fonts
Icons
Images

Controls
StatusBadge

Converters
BoolToVisibilityConverter
FilterChipBackgroundConverter
FilterChipForegroundConverter
LivefeedLevelBrushConverter
ProxyGlyphConverter
RecordTypeBrushConverter

Data
AppDbContext
AppPaths
Migrations

Models
AppSettings
CloudflareZone
DnsRecord
LivefeedEntry
UpdatePackageInfo

Services
CloudflareApiService
DnsRecordService
DpapiSecretService
ISecretService
LivefeedService
SettingsService
UpdateService
WindowHandleProvider

Themes
Colors.xaml
Typography.xaml
Buttons.xaml
Inputs.xaml
Tables.xaml
Controls.xaml

ViewModels
ShellViewModel
CloudflareSettingsViewModel
DnsRecordsViewModel
UpdateViewModel

Views
ShellView
DnsRecordsView
SettingsView
UpdateView

## Architecture

The app uses MVVM with CommunityToolkit.Mvvm.

Views contain layout only.
ViewModels expose state, ObservableCollections and commands.
Services contain Cloudflare API, DNS record operations, settings persistence, update preparation, livefeed logging and secret storage.
SQLite stores non secret settings, stored zones and optional livefeed state.
Cloudflare API calls are isolated in CloudflareApiService and use HttpClientFactory.

## Secret storage

The Cloudflare API token is not stored in SQLite.
DpapiSecretService stores it as an encrypted blob under the current Windows user profile using DPAPI CurrentUser scope.
The file is stored in LocalAppData/CrimsonDnsManager/secrets.dat.
Logs sanitise Authorization and Bearer strings.

## Branding assets

Assets/Icons/app.ico contains the Windows app icon.
Assets/Images/favicon-128-removebg-preview.png is used in the topbar and as source icon image.
Assets/Images/crimson-wordmark.png is used as the topbar wordmark.

The icon and wordmark are not recolored or redrawn.

## Fonts

The XAML resources reference local Sora and IBM Plex Mono font files under Assets/Fonts.
Font files are intentionally not included in this archive.
Place the following files before final packaging:

Sora-Regular.ttf
Sora-Medium.ttf
Sora-SemiBold.ttf
Sora-Bold.ttf
IBMPlexMono-Regular.ttf
IBMPlexMono-Medium.ttf
IBMPlexMono-SemiBold.ttf

## Start

Open CrimsonDnsManager.sln in Visual Studio 2022 on Windows.
Install the .NET 8 SDK and the Windows App SDK workload.
Set platform to x64.
Restore NuGet packages.
Run CrimsonDnsManager.App.

## Build

From a Windows developer shell:

```powershell
dotnet restore .\CrimsonDnsManager.sln
dotnet build .\CrimsonDnsManager.sln -c Release -p:Platform=x64
```

## Database migrations

The app calls Database.MigrateAsync on startup.
Create the initial migration on Windows with:

```powershell
dotnet ef migrations add InitialCreate --project .\CrimsonDnsManager.App
dotnet ef database update --project .\CrimsonDnsManager.App
```

## Implemented

Native WinUI 3 shell based on the supplied HTML reference.
Exact core color resources from the HTML reference.
Custom button, input, card, stat pill, table and badge styling.
Topbar with provided C icon and provided Cr1ms0n wordmark.
Sidebar with Cloudflare settings, zones, quick actions and update card.
Main DNS record view with header, stats, filters, editable record rows, selection bar, delete modal and livefeed.
Cloudflare token storage through DPAPI.
Cloudflare connection test, zone loading, DNS record loading, create, update, delete and dirty save flow.
SQLite persistence for non secret settings and stored zones.
Update package UI with package pick, verify and install preparation status.

## Open points

Final pixel review must be done on Windows because this environment cannot run WinUI 3.
The local font files must be added before packaging.
The initial EF migration should be generated on Windows after NuGet restore.
The updater has a verified service and UI flow, but the final signed installer handoff is intentionally prepared only.
A complete MSIX or Inno Setup installer can be added after the app is compiled and visually checked on Windows.
