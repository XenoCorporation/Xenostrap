using System;
using System.Threading;
using Xenostrap.Core;
using Xenostrap.Extensions;
using Xenostrap.Platform;
using Xenostrap.Platform.Linux;
using Xenostrap.Platform.MacOS;

namespace Xenostrap.Utility
{
    public static class Platform
    {
        private static readonly Lazy<IPlatformHost?> RuntimeHostValue = new(CreateRuntimeHost, LazyThreadSafetyMode.ExecutionAndPublication);

        public static readonly bool IsWindows = OperatingSystem.IsWindows();
        public static readonly bool IsMacOS = OperatingSystem.IsMacOS();
        public static readonly bool IsLinux = OperatingSystem.IsLinux();

        public static IPlatformHost? RuntimeHost => RuntimeHostValue.Value;

        public static bool SupportsOverlays => IsWindows;
        public static bool SupportsWebBrowser => IsWindows;
        public static bool SupportsInputHooks => IsWindows;
        public static bool SupportsRegistry => IsWindows;
        public static bool SupportsTrayIcon => IsWindows;
        public static bool SupportsAudioDucking => IsWindows;
        public static bool SupportsWindowsClient => IsWindows;

        private static IPlatformHost? CreateRuntimeHost()
        {
            if (IsLinux)
            {
                return new LinuxPlatformHost(new SystemProcessService(), LinuxGithubPlatformUpdater.Create());
            }

            if (IsMacOS)
            {
                return new MacOSPlatformHost();
            }

            return null;
        }
    }
}
