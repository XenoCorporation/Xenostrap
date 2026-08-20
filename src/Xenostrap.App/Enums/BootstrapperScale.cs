using Xenostrap.Models.Attributes;

namespace Xenostrap.Enums;

public enum BootstrapperScale
{
	[EnumName(StaticName = "Compact")]
	Compact,
	[EnumName(StaticName = "Normal")]
	Normal,
	[EnumName(StaticName = "Large")]
	Large
}
