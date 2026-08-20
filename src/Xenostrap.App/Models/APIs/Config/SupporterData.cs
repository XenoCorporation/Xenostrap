using System.Text.Json.Serialization;

namespace Xenostrap.Models.APIs.Config;

public class SupporterData
{
	[JsonPropertyName("monthly")]
	public SupporterGroup Monthly { get; set; } = new SupporterGroup();

	[JsonPropertyName("oneoff")]
	public SupporterGroup OneOff { get; set; } = new SupporterGroup();
}
