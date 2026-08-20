using System.Collections.Generic;

namespace Xenostrap.Models.APIs;

public sealed class ServerListResponse
{
	public string? NextPageCursor { get; set; }

	public List<ServerInfo> Data { get; set; } = new List<ServerInfo>();
}
