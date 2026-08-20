using Xenostrap.Models.Attributes;

namespace Xenostrap.Enums;

public enum Theme
{
	[EnumName(FromTranslation = "Common.SystemDefault")]
	Default,
	Dark,
	Light,
	Xenostrap,
	UltraGray,
	Berry,
	Blue,
	Cyan,
	Green,
	Orange,
	Pink,
	Purple,
	Red,
	Yellow,
	Custom
}
