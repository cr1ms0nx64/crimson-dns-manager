using CrimsonDnsManager.App.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;
using CrimsonDnsManager.App.Models;
using CrimsonDnsManager.App.Services;
using CrimsonDnsManager.App.ViewModels;

var passed = 0;
async Task Check(string name, Func<Task> test)
{
    await test();
    Console.WriteLine($"PASS {name}");
    passed++;
}
void Expect(bool value, string message = "Assertion failed") { if (!value) throw new Exception(message); }
async Task Throws(Func<Task> action)
{
    try { await action(); } catch (HttpRequestException) { return; }
    throw new Exception("Expected rejected API response");
}
DnsRecord Record(string id = "record-one", string name = "host.example.test") => new()
{ Id = id, Name = name, Type = "A", Content = "192.0.2.1", Ttl = 1 };
HttpResponseMessage Json(object payload, HttpStatusCode status = HttpStatusCode.OK) => new(status)
{ Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json") };
CloudflareApiService Api(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send, LivefeedService? feed = null) => new(
    new HttpClient(new Handler(send)) { BaseAddress = new Uri("https://api.example.test/") }, new Secrets(), feed ?? new());
DnsRecordsViewModel Vm(CloudflareApiService api) => new(new(api, new()), new(), new());

await Check("DNS pagination includes later pages", async () =>
{
    var requests = new List<string>();
    var api = Api((r, _) => { requests.Add(r.RequestUri!.Query); return Task.FromResult(Json(new
    { success = true, result = new[] { Record(requests.Count.ToString()) }, result_info = new { total_pages = 3 } })); });
    Expect((await api.GetDnsRecordsAsync("zone")).Count == 3);
    Expect(requests.Last().Contains("page=3"));
});
await Check("Zones use pagination and escape domain", async () =>
{
    var count = 0;
    var api = Api((r, _) => { count++; Expect(r.RequestUri!.Query.Contains("name=example.test%26page%3D9")); return Task.FromResult(Json(new
    { success = true, result = new[] { new CloudflareZone { Id = count.ToString() } }, result_info = new { total_pages = 2 } })); });
    Expect((await api.GetZonesAsync("example.test&page=9")).Count == 2);
});
await Check("An empty intermediate page rejects incomplete results", async () =>
{
    await Throws(() => Api((_, _) => Task.FromResult(Json(new { success = true, result = Array.Empty<DnsRecord>(), result_info = new { total_pages = 2 } }))).GetDnsRecordsAsync("zone"));
});
foreach (var op in new[] { "list", "create", "update", "patch", "delete" })
    await Check($"HTTP 200 with success false rejects {op}", async () =>
    {
        var api = Api((_, _) => Task.FromResult(Json(new { success = false, result = (object?)null })));
        var original = Record(); var edited = original.Clone(); edited.Name = "changed.example.test";
        await Throws(() => op switch
        {
            "list" => api.GetDnsRecordsAsync("zone"), "create" => api.CreateRecordAsync("zone", original),
            "update" => api.UpdateRecordAsync("zone", original), "patch" => api.PatchRecordAsync("zone", original, edited),
            _ => api.DeleteRecordAsync("zone", original)
        });
    });
await Check("Missing result cannot report a successful save", async () =>
{
    await Throws(() => Api((_, _) => Task.FromResult(Json(new { success = true }))).CreateRecordAsync("zone", Record()));
});
await Check("Raw HTTP error bodies never enter exception logs", async () =>
{
    var api = Api((_, _) => Task.FromResult(Json(new { provider_echo = "sensitive-test-value" }, HttpStatusCode.Forbidden)));
    try { await api.GetDnsRecordsAsync("zone"); } catch (HttpRequestException ex) { Expect(!ex.Message.Contains("sensitive-test-value")); return; }
    throw new Exception("Expected HTTP rejection");
});
await Check("Inactive API token is not a successful connection", async () =>
{
    Expect(!await Api((_, _) => Task.FromResult(Json(new { success = true, result = new { status = "disabled" } }))).TestConnectionAsync());
});
await Check("Full record edits use PATCH to preserve unmanaged metadata", async () =>
{
    var api = Api((r, _) => { Expect(r.Method == HttpMethod.Patch); return Task.FromResult(Json(new { success = true, result = Record() })); });
    await api.UpdateRecordAsync("zone", Record());
});
await Check("No-op edit sends no network request", async () =>
{
    var api = Api((_, _) => throw new Exception("Unexpected request")); var record = Record();
    await api.PatchRecordAsync("zone", record, record.Clone());
});
await Check("Create keeps server ID and subsequent save updates", async () =>
{
    var methods = new List<HttpMethod>();
    var api = Api((r, _) => { methods.Add(r.Method); return Task.FromResult(Json(new { success = true, result = Record("assigned-id") })); });
    var service = new DnsRecordService(api, new()); var record = Record(""); record.IsDirty = true;
    await service.SaveDirtyAsync("zone", new[] { record }, false);
    Expect(record.Id == "assigned-id" && !record.IsDirty);
    record.Name = "changed.example.test"; record.IsDirty = true;
    await service.SaveDirtyAsync("zone", new[] { record }, false);
    Expect(methods.SequenceEqual(new[] { HttpMethod.Post, HttpMethod.Patch }));
});
await Check("Edits made while a save is in flight remain dirty", async () =>
{
    var reply = new TaskCompletionSource<HttpResponseMessage>();
    var record = Record(""); record.IsDirty = true;
    var service = new DnsRecordService(Api((_, _) => reply.Task), new());
    var save = service.SaveDirtyAsync("zone", new[] { record }, false);
    record.Content = "192.0.2.2";
    reply.SetResult(Json(new { success = true, result = Record("assigned-id") }));
    await save;
    Expect(record.Id == "assigned-id" && record.Content == "192.0.2.2" && record.IsDirty);
});
await Check("Failed save keeps unsaved changes", async () =>
{
    var record = Record(); record.IsDirty = true;
    await Throws(() => new DnsRecordService(Api((_, _) => Task.FromResult(Json(new { success = false }))), new()).SaveDirtyAsync("zone", new[] { record }, false));
    Expect(record.IsDirty);
});
await Check("Computed labels notify bindings", () =>
{
    var record = Record(); var names = new List<string?>(); record.PropertyChanged += (_, e) => names.Add(e.PropertyName);
    record.Ttl = 300; record.Priority = 10; record.Type = "TXT";
    Expect(new[] { "DisplayTtl", "DisplayPriority", "CanBeProxied" }.All(names.Contains));
    return Task.CompletedTask;
});
await Check("Invalid IP TTL and priority are rejected before HTTP", async () =>
{
    foreach (var field in new[] { "ip", "ttl", "priority" })
    {
        var record = Record(); if (field == "ip") record.Content = "bad-ip"; else if (field == "ttl") record.Ttl = 0; else record.Priority = -1;
        try { await Api((_, _) => throw new Exception("Unexpected request")).CreateRecordAsync("zone", record); }
        catch (InvalidOperationException) { continue; }
        throw new Exception("Invalid record accepted");
    }
});
await Check("Late old-zone response cannot replace current records", async () =>
{
    var old = new TaskCompletionSource<HttpResponseMessage>(); var latest = new TaskCompletionSource<HttpResponseMessage>();
    var vm = Vm(Api((r, _) => r.RequestUri!.AbsolutePath.Contains("old-zone") ? old.Task : latest.Task));
    var first = vm.SetActiveZoneAsync(new() { Id = "old-zone" });
    var second = vm.SetActiveZoneAsync(new() { Id = "new-zone" });
    old.SetResult(Json(new { success = true, result = new[] { Record("old") } })); await first;
    Expect(vm.IsLoading && vm.Records.Count == 0);
    latest.SetResult(Json(new { success = true, result = new[] { Record("new") } })); await second;
    Expect(!vm.IsLoading && vm.Records.Single().Id == "new");
});
await Check("Reload resets selection and detaches old records", async () =>
{
    var vm = Vm(Api((_, _) => Task.FromResult(Json(new { success = true, result = new[] { Record() } }))));
    await vm.SetActiveZoneAsync(new() { Id = "zone" }); var old = vm.Records.Single(); old.IsSelected = true;
    var notifications = new List<string?>(); vm.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
    await vm.LoadRecordsCommand.ExecuteAsync(null);
    old.Content = "192.0.2.2";
    Expect(!old.IsDirty && !vm.HasSelection && notifications.Contains("SelectedCount"));
});
await Check("Partial deletion refreshes visible rows and selection", async () =>
{
    var deletes = 0;
    var vm = Vm(Api((r, _) => Task.FromResult(r.Method == HttpMethod.Delete
        ? ++deletes == 1 ? Json(new { success = true, result = new { id = "one" } }) : Json(new { success = false })
        : Json(new { success = true, result = new[] { Record("one"), Record("two") } }))));
    await vm.SetActiveZoneAsync(new() { Id = "zone" }); foreach (var record in vm.Records) record.IsSelected = true;
    await vm.ConfirmDeleteCommand.ExecuteAsync(null);
    Expect(vm.Records.Count == 1 && vm.FilteredRecords.Single().Id == "two" && vm.SelectedCount == 1 && !vm.IsSaving);
});
await Check("Filter updates when a record changes type", async () =>
{
    var vm = Vm(Api((_, _) => Task.FromResult(Json(new { success = true, result = new[] { Record() } }))));
    await vm.SetActiveZoneAsync(new() { Id = "zone" }); vm.FilterType = "A"; vm.Records.Single().Type = "TXT";
    Expect(vm.FilteredRecords.Count == 0 && vm.TxtCount == 1);
});
await Check("Zone change waits for an active write and loads the latest selection", async () =>
{
    var reply = new TaskCompletionSource<HttpResponseMessage>();
    var vm = Vm(Api((r, _) => r.Method == HttpMethod.Post ? reply.Task
        : Task.FromResult(Json(new { success = true, result = new[] { Record("zone-record") } }))));
    await vm.SetActiveZoneAsync(new() { Id = "first" });
    vm.DraftRecord = Record("");
    var create = vm.CreateRecordCommand.ExecuteAsync(null);
    await vm.SetActiveZoneAsync(new() { Id = "second" });
    await vm.SetActiveZoneAsync(new() { Id = "third" });
    Expect(vm.ActiveZone!.Id == "first" && vm.IsSaving);
    reply.SetResult(Json(new { success = true, result = Record("created") })); await create;
    Expect(vm.ActiveZone.Id == "third" && !vm.IsSaving && vm.Records.Single().Id == "zone-record");
});
await Check("Delete without a result cannot report success", async () =>
{
    await Throws(() => Api((_, _) => Task.FromResult(Json(new { success = true }))).DeleteRecordAsync("zone", Record()));
});
await Check("Settings persistence serializes concurrent operations", async () =>
{
    using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
    await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    await db.Database.EnsureCreatedAsync();
    var service = new SettingsService(db, new Secrets(), new());
    await service.LoadAsync();
    await Task.WhenAll(Enumerable.Range(0, 12).Select(i => service.SaveAsync(new() { Domain = $"zone-{i}.test", ZoneId = i.ToString() }, null)));
    Expect(await db.Settings.CountAsync() == 1);
    await service.StoreZonesAsync(new[] { new CloudflareZone { Id = "zone", Name = "before.test" } }, "zone");
    await service.StoreZonesAsync(new[] { new CloudflareZone { Id = "zone", Name = "after.test" } }, "zone");
    Expect((await db.StoredZones.SingleAsync()).Name == "after.test");
});
Console.WriteLine($"{passed} checks passed. No real network requests or backups.");

sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
}
sealed class Secrets : ISecretService
{
    public Task<string?> GetCloudflareTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("synthetic-test-token");
    public Task<bool> HasCloudflareTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task SaveCloudflareTokenAsync(string token, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task ClearCloudflareTokenAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
