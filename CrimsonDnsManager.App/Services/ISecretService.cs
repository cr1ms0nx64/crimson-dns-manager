namespace CrimsonDnsManager.App.Services;

public interface ISecretService
{
    Task SaveCloudflareTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<string?> GetCloudflareTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> HasCloudflareTokenAsync(CancellationToken cancellationToken = default);
    Task ClearCloudflareTokenAsync(CancellationToken cancellationToken = default);
}
