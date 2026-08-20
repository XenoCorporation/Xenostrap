using System.Collections.Generic;
using Microsoft.Win32;
using Xenostrap.Enums;

namespace Xenostrap.Extensions;

public static class ThemeEx
{
	public static IReadOnlyCollection<Theme> Selections { get; } = new Theme[15]
	{
		Theme.Default,
		Theme.Custom,
		Theme.Dark,
		Theme.Light,
		Theme.Xenostrap,
		Theme.UltraGray,
		Theme.Blue,
		Theme.Cyan,
		Theme.Green,
		Theme.Orange,
		Theme.Pink,
		Theme.Purple,
		Theme.Berry,
		Theme.Red,
		Theme.Yellow
	};

	public static Theme GetFinal(this Theme dialogTheme)
	{
		if (dialogTheme != Theme.Default)
		{
			return dialogTheme;
		}
		if (!Xenostrap.Utility.Platform.SupportsRegistry)
		{
			return Theme.Dark;
		}
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
		object obj = registryKey?.GetValue("AppsUseLightTheme");
		if (obj is int && (int)obj == 0)
		{
			return Theme.Dark;
		}
		return Theme.Light;
	}
}
