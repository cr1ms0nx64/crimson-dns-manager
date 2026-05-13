using CrimsonDnsManager.App.Models;

namespace CrimsonDnsManager.App.Services;

public sealed class UpdateService(LivefeedService livefeed)
{
    public UpdatePackageInfo Current { get; } = new();

    public Task SetPackageAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
        {
            Current.PackagePath = null;
            Current.IsVerified = false;
            Current.StatusText = "Update Paket nicht gefunden.";
            livefeed.Warning(Current.StatusText);
            return Task.CompletedTask;
        }

        Current.PackagePath = packagePath;
        Current.IsVerified = false;
        Current.StatusText = Path.GetFileName(packagePath);
        livefeed.Info($"Update Paket gewählt: {Path.GetFileName(packagePath)}");
        return Task.CompletedTask;
    }

    public Task<bool> VerifyAsync(CancellationToken cancellationToken = default)
    {
        if (!Current.HasPackage)
        {
            Current.StatusText = "kein Update Paket gewählt.";
            livefeed.Warning(Current.StatusText);
            return Task.FromResult(false);
        }

        var packagePath = Current.PackagePath ?? string.Empty;
        var extension = Path.GetExtension(packagePath);
        var validExtension = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase);

        Current.IsVerified = validExtension;
        Current.StatusText = validExtension
            ? "Paket geprüft und bereit."
            : "Paketformat nicht unterstützt.";

        if (validExtension)
        {
            livefeed.Success("Update Paket geprüft.");
        }
        else
        {
            livefeed.Error("Update Paketprüfung fehlgeschlagen.");
        }

        return Task.FromResult(validExtension);
    }

    public Task InstallAndRestartAsync(CancellationToken cancellationToken = default)
    {
        if (!Current.HasPackage)
        {
            Current.StatusText = "kein Update Paket gewählt.";
            livefeed.Warning(Current.StatusText);
            return Task.CompletedTask;
        }

        if (!Current.IsVerified)
        {
            Current.StatusText = "Paket muss zuerst geprüft werden.";
            livefeed.Warning(Current.StatusText);
            return Task.CompletedTask;
        }

        Current.StatusText = "Installation vorbereitet. Finaler Installer Hook fehlt noch.";
        livefeed.Info(Current.StatusText);
        return Task.CompletedTask;
    }
}

