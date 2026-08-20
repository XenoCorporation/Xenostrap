using System.Text.Json.Serialization;

namespace Xenostrap.Models;

public class FontFace
{
	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("weight")]
	public int Weight { get; set; }

	[JsonPropertyName("style")]
	public string Style { get; set; }

	[JsonPropertyName("assetId")]
	public string AssetId { get; set; }
}
