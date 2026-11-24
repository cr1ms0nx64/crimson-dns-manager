# Crimson DNS Manager

Windows-App zur Verwaltung von Cloudflare-DNS: Zonen laden, Einträge suchen, erstellen, bearbeiten und nach Bestätigung löschen.

**Technik:** C# · .NET 8 · WinUI 3 · MVVM · SQLite

## Einrichtung

Windows 10 ab Version 2004 oder Windows 11, .NET 8 SDK und Visual Studio 2022 mit WinUI-/Windows-App-SDK-Unterstützung werden benötigt. Zielplattform: **x64**.

Die Skripte verwenden Visual-Studio-MSBuild, damit auch die benötigten PRI-/Windows-Paketierungsaufgaben verfügbar sind.

```powershell
.\build.ps1
.\run.ps1
```

In der App einen Cloudflare-API-Token mit Zugriff auf die gewünschten Zonen und DNS-Einträge hinterlegen. Änderungen werden über die Cloudflare-API ausgeführt.

## Lokale Daten

Einstellungen und Zonendaten liegen in SQLite unter `%LOCALAPPDATA%\CrimsonDnsManager`. Der API-Token wird separat mit Windows-DPAPI für den aktuellen Benutzer in `secrets.dat` geschützt. Zugangsdaten, Datenbanken und Logs gehören nicht ins Repository.

## Projektstand

- DNS-Verwaltung mit Filtern, Mehrfachauswahl und Livefeed.
- Die Datenbank wird beim Start mit `EnsureCreated` angelegt; ein versionierter EF-Migrationspfad ist nicht eingerichtet.
- Die Update-Oberfläche ist vorbereitet. Aktuell wird nur die Dateiendung geprüft; Signaturprüfung und Installer-Anbindung fehlen.

## Entwicklung

Die Anwendung liegt in `CrimsonDnsManager.App`; Views, ViewModels, Services und Datenmodell sind getrennt. Die mitgelieferten Schriftarten **Sora** und **IBM Plex Mono** stehen unter der [SIL Open Font License](CrimsonDnsManager.App/Assets/Fonts/README.md).

Gezielter Test der Log-Maskierung:

```powershell
dotnet run --project tests/LogRedaction/LogRedaction.csproj -c Release
```
