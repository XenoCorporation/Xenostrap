using Xenostrap.Models.Attributes;

namespace Xenostrap.Enums.FlagPresets;

public enum LightingMode
{
	Default,
	Voxel,
	ShadowMap,
	Future,
	[EnumName(StaticName = "Unified (Phase 4)")]
	Unified
}
