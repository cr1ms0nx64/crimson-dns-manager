using CrimsonDnsManager.App.Models;
using System.Collections.ObjectModel;

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

        return message
            .Replace("Authorization", "Auth", StringComparison.OrdinalIgnoreCase)
            .Replace("Bearer ", "Bearer ***", StringComparison.OrdinalIgnoreCase);
    }
}
