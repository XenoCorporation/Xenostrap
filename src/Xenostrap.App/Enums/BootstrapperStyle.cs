using Xenostrap.Models.Attributes;

namespace Xenostrap.Enums;

public enum BootstrapperStyle
{
	VistaDialog,
	LegacyDialog2008,
	LegacyDialog2011,
	ProgressDialog,
	ClassicFluentDialog,
	ByfronDialog,
	[EnumName(StaticName = "Xenostrap")]
	FluentDialog,
	FluentAeroDialog,
	CustomDialog
}
