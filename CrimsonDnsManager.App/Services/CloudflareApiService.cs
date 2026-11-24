using CrimsonDnsManager.App.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrimsonDnsManager.App.Services;

public sealed class CloudflareApiService(HttpClient httpClient, ISecretService secretService, LivefeedService livefeed)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "user/tokens/verify", cancellationToken);
        livefeed.Action("Cloudflare API: GET /user/tokens/verify");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            livefeed.Error($"Verbindungstest fehlgeschlagen. HTTP {(int)response.StatusCode}");
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<CloudflareResponse<TokenVerifyResult>>(JsonOptions, cancellationToken);
        var ok = payload?.Success == true && payload.Result?.Status == "active";
        if (ok)
        {
            livefeed.Success("Cloudflare Verbindung erfolgreich getestet.");
        }
        else
        {
            livefeed.Warning("Cloudflare Verbindungstest ohne Erfolg abgeschlossen.");
        }

        return ok;
    }

    public async Task<IReadOnlyList<CloudflareZone>> GetZonesAsync(string? domain, CancellationToken cancellationToken = default)
    {
        var route = string.IsNullOrWhiteSpace(domain)
            ? "zones?per_page=50"
            : $"zones?name={Uri.EscapeDataString(domain.Trim())}&per_page=50";
        var zones = await GetAllPagesAsync<CloudflareZone>(route, cancellationToken);
        livefeed.Success($"Zonen geladen: {zones.Count}");
        return zones;
    }

    public async Task<IReadOnlyList<DnsRecord>> GetDnsRecordsAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            throw new InvalidOperationException("Keine Zone ID gesetzt.");

        var records = await GetAllPagesAsync<DnsRecord>($"zones/{zoneId}/dns_records?per_page=100", cancellationToken);
        livefeed.Success($"DNS Records geladen. Records: {records.Count}");
        return records;
    }

    private async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(string route, CancellationToken cancellationToken)
    {
        var results = new List<T>();
        for (var page = 1; ; page++)
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, $"{route}&page={page}", cancellationToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var payload = await ReadSuccessAsync<List<T>>(response, cancellationToken);
            results.AddRange(payload.Result!);
            if (page >= (payload.ResultInfo?.TotalPages ?? 1)) return results;
            if (payload.Result!.Count == 0)
                throw new HttpRequestException("Cloudflare lieferte eine leere Zwischenseite. Bitte erneut laden.");
        }
    }

    public async Task<DnsRecord> CreateRecordAsync(string zoneId, DnsRecord record, CancellationToken cancellationToken = default)
    {
        ValidateRecord(record);
        var route = $"zones/{zoneId}/dns_records";
        using var request = await CreateJsonRequestAsync(HttpMethod.Post, route, ToPayload(record), cancellationToken);
        livefeed.Action($"Cloudflare API: POST /zones/{ShortId(zoneId)}/dns_records");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadSuccessAsync<DnsRecord>(response, cancellationToken);
        livefeed.Success($"Record erstellt: {record.Name} {record.Type}");
        return payload.Result!;
    }

    public async Task<DnsRecord> UpdateRecordAsync(string zoneId, DnsRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(record.Id))
        {
            throw new InvalidOperationException("Record hat keine Cloudflare ID.");
        }

        ValidateRecord(record);
        var route = $"zones/{zoneId}/dns_records/{record.Id}";
        using var request = await CreateJsonRequestAsync(HttpMethod.Patch, route, ToPayload(record), cancellationToken);
        livefeed.Action($"Cloudflare API: PATCH /zones/{ShortId(zoneId)}/dns_records/{ShortId(record.Id)}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadSuccessAsync<DnsRecord>(response, cancellationToken);
        livefeed.Success($"Record geändert: {record.Name} {record.Type}");
        return payload.Result!;
    }

    public async Task<DnsRecord> PatchRecordAsync(string zoneId, DnsRecord original, DnsRecord updated, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(original.Id))
        {
            throw new InvalidOperationException("Record hat keine Cloudflare ID.");
        }

        updated.Id = original.Id;
        ValidateRecord(updated);

        var patchPayload = ToPatchPayload(original, updated);
        if (patchPayload.Count == 0)
        {
            livefeed.Info($"Keine Änderungen für Record: {original.Name} {original.Type}");
            return original;
        }

        var route = $"zones/{zoneId}/dns_records/{original.Id}";
        using var request = await CreateJsonRequestAsync(HttpMethod.Patch, route, patchPayload, cancellationToken);
        livefeed.Action($"Cloudflare API: PATCH /zones/{ShortId(zoneId)}/dns_records/{ShortId(original.Id)}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadSuccessAsync<DnsRecord>(response, cancellationToken);
        livefeed.Success($"Record bearbeitet: {updated.Name} {updated.Type}");
        return payload.Result!;
    }

    public async Task DeleteRecordAsync(string zoneId, DnsRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(record.Id))
        {
            throw new InvalidOperationException("Record hat keine Cloudflare ID.");
        }

        var route = $"zones/{zoneId}/dns_records/{record.Id}";
        using var request = await CreateRequestAsync(HttpMethod.Delete, route, cancellationToken);
        livefeed.Action($"Cloudflare API: DELETE /zones/{ShortId(zoneId)}/dns_records/{ShortId(record.Id)}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ReadSuccessAsync<JsonElement>(response, cancellationToken);
        livefeed.Success($"Record gelöscht: {record.Name} {record.Type}");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string route, CancellationToken cancellationToken)
    {
        var token = await secretService.GetCloudflareTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Kein Cloudflare API Token gespeichert.");
        }

        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<HttpRequestMessage> CreateJsonRequestAsync(HttpMethod method, string route, object payload, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(method, route, cancellationToken);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        return request;
    }

    private static object ToPayload(DnsRecord record)
    {
        var proxied = record.CanBeProxied && record.Proxied;
        return new
        {
            type = record.Type,
            name = record.Name,
            content = record.Content,
            ttl = record.Ttl <= 0 ? 1 : record.Ttl,
            priority = record.Priority,
            proxied
        };
    }

    private static Dictionary<string, object?> ToPatchPayload(DnsRecord original, DnsRecord updated)
    {
        var patch = new Dictionary<string, object?>();
        var ttl = updated.Ttl <= 0 ? 1 : updated.Ttl;
        var originalTtl = original.Ttl <= 0 ? 1 : original.Ttl;
        var proxied = updated.CanBeProxied && updated.Proxied;
        var originalProxied = original.CanBeProxied && original.Proxied;

        if (!string.Equals(original.Type, updated.Type, StringComparison.Ordinal))
        {
            patch["type"] = updated.Type;
        }

        if (!string.Equals(original.Name, updated.Name, StringComparison.Ordinal))
        {
            patch["name"] = updated.Name;
        }

        if (!string.Equals(original.Content, updated.Content, StringComparison.Ordinal))
        {
            patch["content"] = updated.Content;
        }

        if (originalTtl != ttl)
        {
            patch["ttl"] = ttl;
        }

        if (original.Priority != updated.Priority)
        {
            patch["priority"] = updated.Priority;
        }

        if (originalProxied != proxied)
        {
            patch["proxied"] = proxied;
        }

        return patch;
    }

    public static void ValidateRecord(DnsRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Type))
        {
            throw new InvalidOperationException("Record Typ fehlt.");
        }

        if (string.IsNullOrWhiteSpace(record.Name))
        {
            throw new InvalidOperationException("Record Name fehlt.");
        }

        if (string.IsNullOrWhiteSpace(record.Content))
        {
            throw new InvalidOperationException("Record Content fehlt.");
        }

        if (record.Ttl != 1 && (record.Ttl < 30 || record.Ttl > 86400))
            throw new InvalidOperationException("TTL muss Auto (1) oder zwischen 30 und 86400 Sekunden sein.");
        if (record.Priority is < 0 or > 65535)
            throw new InvalidOperationException("Priority muss zwischen 0 und 65535 liegen.");
        if (record.Type is "A" or "AAAA")
        {
            var family = record.Type == "A" ? System.Net.Sockets.AddressFamily.InterNetwork : System.Net.Sockets.AddressFamily.InterNetworkV6;
            if (!System.Net.IPAddress.TryParse(record.Content, out var address) || address.AddressFamily != family)
                throw new InvalidOperationException($"Ungültige {record.Type}-Adresse.");
        }

        if (record.Type is "MX" or "SRV" && record.Priority is null)
        {
            throw new InvalidOperationException("Priority ist für diesen Record Typ erforderlich.");
        }

        if (!record.CanBeProxied)
        {
            record.Proxied = false;
        }
    }

    private static async Task<CloudflareResponse<T>> ReadSuccessAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Cloudflare API Fehler HTTP {(int)response.StatusCode}.", null, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CloudflareResponse<T>>(JsonOptions, cancellationToken);
        if (payload?.Success != true || payload.Result is null
            || payload.Result is JsonElement element && element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new HttpRequestException("Cloudflare hat den Vorgang nicht bestätigt. Bitte Berechtigungen und Eingaben prüfen.");
        return payload;
    }

    private static string ShortId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value.Length <= 8 ? value : value[..8] + "…";
    }

    private sealed class CloudflareResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("result")]
        public T? Result { get; set; }

        [JsonPropertyName("result_info")]
        public PageInfo? ResultInfo { get; set; }
    }

    private sealed class PageInfo
    {
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    private sealed class TokenVerifyResult
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
