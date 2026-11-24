using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CrimsonDnsManager.App.Models;

public partial class CloudflareZone : ObservableObject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool isActive;

    public string StatusText => IsActive ? "aktiv" : "bereit";
}
