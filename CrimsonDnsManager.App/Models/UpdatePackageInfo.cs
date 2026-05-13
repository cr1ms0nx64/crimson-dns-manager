namespace CrimsonDnsManager.App.Models;

public sealed class UpdatePackageInfo
{
    public string? PackagePath { get; set; }
    public string StatusText { get; set; } = "kein Update Paket gewählt.";
    public bool HasPackage => !string.IsNullOrWhiteSpace(PackagePath);
    public bool IsVerified { get; set; }
}
