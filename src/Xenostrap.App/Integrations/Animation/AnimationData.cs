using System.Collections.Generic;

namespace Xenostrap.Integrations.Animation;

public sealed class AnimationData
{
	public double Length { get; set; }

	public bool IsR15 { get; set; }

	public List<AnimKeyframe> Keyframes { get; } = [];
}
