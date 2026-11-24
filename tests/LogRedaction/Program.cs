using CrimsonDnsManager.App.Services;

var samples = new[]
{
    ("Authorization: Bearer example-credential\r\nDNS request", "example-credential"),
    ("authorization: Basic ZXhhbXBsZTpleGFtcGxl", "ZXhhbXBsZTpleGFtcGxl"),
    ("Request failed: Bearer example-credential", "example-credential"),
    ("token=example-credential, next=ok", "example-credential"),
    ("password=\"example value with spaces\"", "example value with spaces")
};
foreach (var (input, sensitive) in samples)
{
    var feed = new LivefeedService();
    string? emitted = null;
    feed.EntryAdded += (_, entry) => emitted = entry.Message;
    feed.Info(input);
    if (feed.Entries.Single().Message.Contains(sensitive) || emitted?.Contains(sensitive) != false)
        throw new Exception("Log redaction regression");
}
var plain = new LivefeedService();
plain.Info("DNS records loaded: 12");
if (plain.Entries.Single().Message != "DNS records loaded: 12")
    throw new Exception("Ordinary diagnostic text changed");
Console.WriteLine("Passed: 6 log-redaction checks; collection and event payloads verified.");
