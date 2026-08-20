using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Xenostrap.Resources;
using FontFamily = System.Windows.Media.FontFamily;

namespace Xenostrap;

internal static class Locale
{
	public const string DefaultLocale = "en-US";

	public static readonly Dictionary<string, string> SupportedLocales = new Dictionary<string, string>
	{
		{ "en-US", "English" },
		{ "vi", "Tiếng Việt" }
	};

	public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.InvariantCulture;

	public static bool RightToLeft { get; private set; } = false;

	public static string GetIdentifierFromName(string language)
	{
		return SupportedLocales.FirstOrDefault<KeyValuePair<string, string>>((KeyValuePair<string, string> x) => x.Value == language).Key ?? DefaultLocale;
	}

	public static List<string> GetLanguages()
	{
		return SupportedLocales.Values.ToList();
	}

	public static void Set(string identifier)
	{
		if (!SupportedLocales.ContainsKey(identifier))
		{
			identifier = DefaultLocale;
		}
		try
		{
			CurrentCulture = new CultureInfo(identifier);
		}
		catch (CultureNotFoundException)
		{
			CurrentCulture = CultureInfo.InvariantCulture;
		}
		CultureInfo.DefaultThreadCurrentUICulture = CurrentCulture;
		Thread.CurrentThread.CurrentUICulture = CurrentCulture;
		RightToLeft = false;
		try
		{
			if (App.Settings.Prop.AutoTranslate)
			{
				Xenostrap.UI.LiveLanguageRefresher.RefreshAllOpenWindows();
			}
			else
			{
				Xenostrap.UI.LiveLanguageRefresher.RestoreAllOpenWindows();
			}
		}
		catch
		{
		}
	}

	public static bool IsRightToLeftLanguage(string language)
	{
		return false;
	}

	public static void Initialize()
	{
		Set(DefaultLocale);
		EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, (RoutedEventHandler)delegate(object sender, RoutedEventArgs _)
		{
			Window window = (Window)sender;
			if (RightToLeft)
			{
				window.FlowDirection = FlowDirection.RightToLeft;
				if (window.ContextMenu != null)
				{
					window.ContextMenu.FlowDirection = FlowDirection.RightToLeft;
				}
			}
			else if (CurrentCulture.Name.StartsWith("th"))
			{
				window.FontFamily = new FontFamily(new Uri("pack://application:,,,/Resources/Fonts/"), "./#Noto Sans Thai");
			}
		});
	}
}
