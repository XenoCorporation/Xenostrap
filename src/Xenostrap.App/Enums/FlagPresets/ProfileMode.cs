using Xenostrap.Models.Attributes;

namespace Xenostrap.Enums.FlagPresets;

public enum ProfileMode
{
	[EnumName(FromTranslation = "Common.Automatic")]
	Default,
	[EnumName(StaticName = "Xenostraps Official")]
	Xenostrap,
	[EnumName(StaticName = "Stoofs")]
	Stoof
}
