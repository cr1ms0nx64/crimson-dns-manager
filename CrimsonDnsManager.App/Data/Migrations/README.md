# Datenbankschema

Aktuell verwendet die App `Database.EnsureCreated()`; es sind keine EF-Migrationen vorhanden. Die folgenden Befehle sind daher kein Updateverfahren für eine bestehende Datenbank.

Für eine spätere Umstellung zuerst einen Migrationspfad für vorhandene Installationen festlegen. Die erste Migration wird anschließend mit den EF-Werkzeugen erstellt:

```powershell
dotnet ef migrations add InitialCreate --project CrimsonDnsManager.App
```
