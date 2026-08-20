using Avalonia;
using Xenostrap.Desktop;
using Xenostrap.Platform.MacOS;

namespace Xenostrap.Desktop.MacOS;

internal static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		DesktopRuntime.Configure(new MacOSPlatformHost(), args);
		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<DesktopApplication>().UsePlatformDetect();
	}
}
