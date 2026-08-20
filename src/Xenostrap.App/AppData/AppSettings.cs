using Xenostrap.Enums;

namespace Xenostrap.AppData;

public class AppSettings
{
	public string CustomFontLocation { get; set; } = string.Empty;

	public CursorType CursorType { get; set; }

	public bool UseFastFlagManager { get; set; }

	public bool XenostrapRPCReal { get; set; }

	public bool WPFSoftwareRender { get; set; }

	public string Locale { get; set; } = "en-US";

	public string? SelectedCustomTheme { get; set; }
}
