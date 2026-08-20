using System.Text.Json.Serialization;

namespace XenostrapClient.WebServer;

internal class AssetLocation
{
	[JsonPropertyName("location")]
	public string? Location { get; set; }
}
