using System;
using System.Collections.Generic;

namespace Xenostrap.Integrations.Animation;

public sealed class AnimKeyframe
{
	public double Time { get; init; }

	public Dictionary<string, RobloxCFrame> Poses { get; } = new Dictionary<string, RobloxCFrame>(StringComparer.Ordinal);
}
