using System.Text.Json.Serialization;

namespace XenostrapClient.WebServer.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CharacterLoadType
{
	Fetch,
	Whole
}
