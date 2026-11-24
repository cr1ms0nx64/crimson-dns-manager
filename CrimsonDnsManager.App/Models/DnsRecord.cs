using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CrimsonDnsManager.App.Models;

public partial class DnsRecord : ObservableObject
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBeProxied))]
    [JsonPropertyName("type")]
    private string type = "A";

    [ObservableProperty]
    [JsonPropertyName("name")]
    private string name = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("content")]
    private string content = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTtl))]
    [JsonPropertyName("ttl")]
    private int ttl = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayPriority))]
    [JsonPropertyName("priority")]
    private int? priority;

    [ObservableProperty]
    [JsonPropertyName("proxied")]
    private bool proxied;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isDirty;

    public string DisplayTtl => Ttl == 1 ? "Auto" : Ttl.ToString();
    public string DisplayPriority => Priority.HasValue ? Priority.Value.ToString() : "—";
    public bool CanBeProxied => Type is "A" or "AAAA" or "CNAME";

    public DnsRecord Clone() => new()
    {
        Id = Id,
        Type = Type,
        Name = Name,
        Content = Content,
        Ttl = Ttl,
        Priority = Priority,
        Proxied = Proxied,
        IsDirty = IsDirty,
        IsSelected = IsSelected
    };
}
