using System;
using System.Collections.Generic;

namespace Xenostrap.Integrations;

public sealed class ServerFetchData
{
	public Dictionary<string, LearnedServerEntry> Servers { get; set; } = new Dictionary<string, LearnedServerEntry>(StringComparer.OrdinalIgnoreCase);

	public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

	public int SchemaVersion { get; set; } = 1;
}
