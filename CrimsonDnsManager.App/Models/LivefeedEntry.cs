namespace CrimsonDnsManager.App.Models;

public enum LivefeedLevel
{
    Info,
    Success,
    Warning,
    Error,
    Action
}

public sealed class LivefeedEntry
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public LivefeedLevel Level { get; set; } = LivefeedLevel.Info;
    public string Message { get; set; } = string.Empty;

    public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string LevelText => Level switch
    {
        LivefeedLevel.Success => "OK",
        LivefeedLevel.Warning => "WARN",
        LivefeedLevel.Error => "ERR",
        LivefeedLevel.Action => "ACT",
        _ => "INFO"
    };
}
