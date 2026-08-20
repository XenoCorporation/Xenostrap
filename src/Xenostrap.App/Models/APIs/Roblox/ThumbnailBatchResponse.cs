using System;
using System.Text.Json.Serialization;

namespace Xenostrap.Models.APIs.Roblox;

internal class ThumbnailBatchResponse
{
	[JsonPropertyName("data")]
	public ThumbnailResponse[] Data { get; set; } = Array.Empty<ThumbnailResponse>();
}
