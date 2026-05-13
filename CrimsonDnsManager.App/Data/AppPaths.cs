namespace CrimsonDnsManager.App.Data;

public static class AppPaths
{
    public static string RootPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CrimsonDnsManager");

    public static string DatabasePath => Path.Combine(RootPath, "crimson-dns-manager.db");
    public static string SecretPath => Path.Combine(RootPath, "secrets.dat");
    public static string BackupPath => Path.Combine(RootPath, "Backups");
    public static string UpdatePath => Path.Combine(RootPath, "Updates");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(BackupPath);
        Directory.CreateDirectory(UpdatePath);
    }
}
