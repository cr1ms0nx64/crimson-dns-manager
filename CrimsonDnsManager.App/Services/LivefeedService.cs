using CrimsonDnsManager.App.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CrimsonDnsManager.App.Services;

public sealed class LivefeedService
{
    private const int MaxEntries = 250;
    public ObservableCollection<LivefeedEntry> Entries { get; } = [];

    public event EventHandler<LivefeedEntry>? EntryAdded;

    public void Info(string message) => Add(LivefeedLevel.Info, message);
    public void Success(string message) => Add(LivefeedLevel.Success, message);
    public void Warning(string message) => Add(LivefeedLevel.Warning, message);
    public void Error(string message) => Add(LivefeedLevel.Error, message);
    public void Action(string message) => Add(LivefeedLevel.Action, message);

    public void Add(LivefeedLevel level, string message)
    {
        var entry = new LivefeedEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Message = Sanitise(message)
        };

        Entries.Add(entry);

        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(0);
        }

        EntryAdded?.Invoke(this, entry);
    }

    public void Clear() => Entries.Clear();

    private static string Sanitise(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var safe = Regex.Replace(message, @"\bAuthorization\s*:\s*[^\r\n]+",
            "Authorization: [redacted]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        safe = Regex.Replace(safe, @"\b(Bearer|Basic)\s+[A-Za-z0-9+/_=.\-]+",
            "$1 [redacted]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        return Regex.Replace(safe,
            "(\\b(?:api[-_ ]?token|token|api[-_ ]?key|password|secret|x-auth-key)\\b\\s*[:=]\\s*)(?:\"[^\"]*\"|'[^']*'|[^\\s,;]+)",
            "$1[redacted]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
    }
}
