using CrimsonDnsManager.App.Data;
using System.Security.Cryptography;
using System.Text;

namespace CrimsonDnsManager.App.Services;

public sealed class DpapiSecretService : ISecretService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CrimsonDnsManager.CloudflareToken.v1");

    public async Task SaveCloudflareTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            await ClearCloudflareTokenAsync(cancellationToken);
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI secret storage requires Windows.");
        }

        AppPaths.EnsureCreated();
        var plainBytes = Encoding.UTF8.GetBytes(token.Trim());
        var cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(AppPaths.SecretPath, cipherBytes, cancellationToken);
    }

    public async Task<string?> GetCloudflareTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(AppPaths.SecretPath))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI secret storage requires Windows.");
        }

        var cipherBytes = await File.ReadAllBytesAsync(AppPaths.SecretPath, cancellationToken);
        var plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public Task<bool> HasCloudflareTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(AppPaths.SecretPath));
    }

    public Task ClearCloudflareTokenAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(AppPaths.SecretPath))
        {
            File.Delete(AppPaths.SecretPath);
        }

        return Task.CompletedTask;
    }
}
