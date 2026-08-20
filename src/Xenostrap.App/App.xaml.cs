using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using DiscordRPC;
using Xenostrap.Enums;
using Xenostrap.Extensions;
using Xenostrap.Helpers;
using Xenostrap.Integrations;
using Xenostrap.Integrations.AssetProxy;
using Xenostrap.Models.APIs.GitHub;
using Xenostrap.Models.Attributes;
using Xenostrap.Models.Persistable;
using Xenostrap.Models.SettingTasks.Base;
using Xenostrap.UI;
using Xenostrap.UI.Elements.ContextMenu;
using Xenostrap.UI.ViewModels.ContextMenu;
using Xenostrap.Utility;
using LinqExpression = System.Linq.Expressions.Expression;
using ParameterExpression = System.Linq.Expressions.ParameterExpression;

namespace Xenostrap;

public partial class App : Application
{
	public const string ProjectName = "Xenostrap";

	public const string ProjectOwner = "Xenostrap";

	public const string ProjectRepository = "/XenoCorporation/Xenostrap/";

	public const string ProjectDownloadLink = "https://github.com/XenoCorporation/Xenostrap/releases";
	public const string ProjectFallbackRepository = "https://github.com/XenoCorporation/Xenostrap";
	public const string ProjectFallbackDownloadLink = ProjectFallbackRepository + "/releases";
	public const string ProjectReleaseApi = "https://api.github.com/repos/XenoCorporation/Xenostrap/releases/latest";
	public const string ProjectFallbackReleaseApi = "https://api.github.com/repos/XenoCorporation/Xenostrap/releases/latest";

	public const string ProjectReleaseListApi = "https://api.github.com/repos/XenoCorporation/Xenostrap/releases?per_page=20";

	public const string ProjectFallbackReleaseListApi = "https://api.github.com/repos/XenoCorporation/Xenostrap/releases?per_page=20";

	public const string ProjectHelpLink = "https://xenostrapp.pages.dev/documentation";

	public const string ProjectSupportLink = "https://github.com/XenoCorporation/Xenostrap/issues/new";
	public const string ProjectFallbackSupportLink = ProjectFallbackRepository + "/issues/new";
	public const string ProjectIssuesLink = "https://github.com/XenoCorporation/Xenostrap/issues";
	public const string ProjectFallbackIssuesLink = ProjectFallbackRepository + "/issues";

	public const string RobloxPlayerAppName = "RobloxPlayerBeta";

	public const string RobloxStudioAppName = "RobloxStudioBeta";

	public const string UninstallKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Xenostrap";

	public const string ApisKey = "Software\\Xenostrap";

	// for dev testing new features I may add in the near future

	public static readonly bool UseLocalWebsite = false;

	public const string WebsiteProductionUrl = "https://xenostrapp.pages.dev";

	public const string WebsiteLocalUrl = "http://localhost:8788";

	private static long _localSiteCheckTicks = -60000;

	private static bool _localSiteOnline;

	private static bool IsLocalWebsiteOnline()
	{
		long tickCount = Environment.TickCount64;
		long sinceCheck = tickCount - System.Threading.Interlocked.Read(ref _localSiteCheckTicks);
		int cacheWindow = _localSiteOnline ? 30000 : 3000;

		if (sinceCheck < cacheWindow)
		{
			return _localSiteOnline;
		}

		System.Threading.Interlocked.Exchange(ref _localSiteCheckTicks, tickCount);
		bool wasOnline = _localSiteOnline;

		try
		{
			Uri uri = new(WebsiteLocalUrl);
			using System.Net.Sockets.TcpClient tcpClient = new();
			Task connectTask = tcpClient.ConnectAsync(uri.Host, uri.Port);
			bool finished = connectTask.Wait(1500);
			if (!finished)
			{
				connectTask.ContinueWith(delegate(Task t)
				{
					_ = t.Exception;
				}, TaskContinuationOptions.OnlyOnFaulted);
			}
			_localSiteOnline = finished && tcpClient.Connected;
		}
		catch
		{
			_localSiteOnline = false;
		}

		if (_localSiteOnline != wasOnline)
		{
			Logger?.WriteLine("App::IsLocalWebsiteOnline", _localSiteOnline
				? "Local website is reachable, using " + WebsiteLocalUrl
				: "Local website is not reachable, using " + WebsiteProductionUrl);
		}

		return _localSiteOnline;
	}

	public static string WebsiteBaseUrl => (UseLocalWebsite && IsLocalWebsiteOnline()) ? WebsiteLocalUrl : WebsiteProductionUrl;

	public static readonly string RobloxCookiesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox\\LocalStorage\\RobloxCookies.dat");

	public static readonly BuildMetadataAttribute BuildMetadata = ResolveBuildMetadata();

	public static string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();

	public const int TaskbarProgressMaximum = 100;

	public static readonly Logger Logger = new();

	public static readonly Dictionary<string, BaseTask> PendingSettingTasks = [];

	public static readonly JsonManager<AppSettings> Settings = new();

	public static readonly JsonManager<DownloadStats> DownloadStats = new();

	public static readonly JsonManager<State> State = new();

	public static readonly JsonManager<RobloxState> RobloxState = new();

	public static readonly FastFlagManager FastFlags = new();

	public static readonly GBSEditor GlobalSettings = new();


	private static readonly HttpClient _httpClient = CreateHttpClient();

	private static int _showingExceptionDialog;
	private static bool _portableToolTipsDisabled;

	private readonly CancellationTokenSource _lifetimeCancellation = new();

	public static DiscordRpcClient? DiscordClient { get; set; }

	public static bool WebsiteTakeoverActive { get; set; }

	public static LaunchSettings LaunchSettings { get; private set; } = null!;

	public static Bootstrapper? Bootstrapper { get; set; } = null;

	public static bool IsActionBuild => !string.IsNullOrEmpty(BuildMetadata.CommitRef);

	public static bool IsProductionBuild
	{
		get
		{
			if (IsActionBuild)
			{
				return BuildMetadata.CommitRef.StartsWith("tag", StringComparison.Ordinal);
			}
			return false;
		}
	}

	public static bool IsStudioVisible => !string.IsNullOrEmpty(State.Prop.Studio.VersionGuid);

	public static HttpClient HttpClient => _httpClient;

	private static HttpClient CreateHttpClient()
	{
		HttpClient client = VpnHttpClient.Create(TimeSpan.FromSeconds(30), log: true);
		client.MaxResponseContentBufferSize = 16 * 1024 * 1024;
		return client;
	}

	public static byte[] ComputeSha256(byte[] data)
	{
		return SHA256.HashData(data);
	}

	public static byte[] ComputeSha256(Stream stream)
	{
		using SHA256 sHA = SHA256.Create();
		return sHA.ComputeHash(stream);
	}

	public static void Terminate(ErrorCode exitCode = ErrorCode.ERROR_SUCCESS)
	{
		if (WebsiteTakeoverActive)
		{
			Logger.WriteLine("App::Terminate", "Termination blocked WebsiteTakeoverActive is true.");
			return;
		}
		Logger.WriteLine("App::Terminate", $"Terminating with exit code {(int)exitCode} ({exitCode})");
		ShutdownApplication((int)exitCode);
	}

	public static void SoftTerminate(ErrorCode exitCode = ErrorCode.ERROR_SUCCESS)
	{
		if (WebsiteTakeoverActive)
		{
			Logger.WriteLine("App::SoftTerminate", "Soft termination blocked WebsiteTakeoverActive is true.");
			return;
		}
		if (LaunchSettings?.WindowAuditFlag.Active == true)
		{
			Logger.WriteLine("App::SoftTerminate", "Soft termination blocked, window audit is running.");
			return;
		}
		int exitCodeNum = (int)exitCode;
		Logger.WriteLine("App::SoftTerminate", $"Terminating with exit code {exitCodeNum} ({exitCode})");
		ShutdownApplication(exitCodeNum);
	}

	public static bool RestartApplication(IReadOnlyList<string> arguments)
	{
		try
		{
			string? executable = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(executable))
			{
				return false;
			}
			ProcessStartInfo startInfo = new()
			{
				FileName = executable,
				UseShellExecute = true
			};
			foreach (string argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}
			using Process? process = Process.Start(startInfo);
			if (process == null)
			{
				return false;
			}
			SoftTerminate();
			return true;
		}
		catch (Exception ex)
		{
			Logger.WriteLine("App::RestartApplication", "Restart failed: " + ex.Message);
			return false;
		}
	}

	private static void ShutdownApplication(int exitCodeNum)
	{
		Application? application = Current;
		if (application == null)
		{
			Environment.Exit(exitCodeNum);
			return;
		}
		void shutdown()
		{
			if (!application.Dispatcher.HasShutdownStarted)
			{
				application.Shutdown(exitCodeNum);
			}
		}
		try
		{
			if (application.Dispatcher.CheckAccess())
			{
				shutdown();
			}
			else
			{
				application.Dispatcher.Invoke(shutdown);
			}
		}
		catch
		{
			Environment.Exit(exitCodeNum);
		}
	}

	private void GlobalExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		e.Handled = true;
		Exception ex = UnwrapException(e.Exception);
		if (Dispatcher.HasShutdownStarted && ex is System.ComponentModel.Win32Exception { NativeErrorCode: 1400 })
		{
			return;
		}
		Logger.WriteLine("App::GlobalExceptionHandler", "An exception occurred");
		if (IsFatalException(ex))
		{
			FinalizeExceptionHandling(e.Exception);
		}
		else
		{
			Logger.WriteException("App::GlobalExceptionHandler", ex);
		}
	}

	private static Exception UnwrapException(Exception ex)
	{
		while (true)
		{
			if (ex is AggregateException aggregate && aggregate.InnerException != null)
			{
				ex = aggregate.InnerException;
				continue;
			}
			if (ex is System.Reflection.TargetInvocationException invocation && invocation.InnerException != null)
			{
				ex = invocation.InnerException;
				continue;
			}
			return ex;
		}
	}

	private static bool IsFatalException(Exception ex)
	{
		return ex is OutOfMemoryException || ex is AccessViolationException || ex is System.Runtime.InteropServices.SEHException || ex is BadImageFormatException || ex is System.Threading.ThreadAbortException;
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		try
		{
			Logger.WriteException("App::UnobservedTaskException", e.Exception);
		}
		catch
		{
		}
		e.SetObserved();
	}

	private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		try
		{
			if (e.ExceptionObject is Exception ex)
			{
				Logger.WriteException("App::DomainUnhandledException", ex);
			}
		}
		catch
		{
		}
	}

	public static void FinalizeExceptionHandling(AggregateException ex)
	{
		foreach (Exception innerException in ex.InnerExceptions)
		{
			Logger.WriteException("App::FinalizeExceptionHandling", innerException);
		}
		FinalizeExceptionHandling(ex.GetBaseException(), log: false);
	}

	public static void FinalizeExceptionHandling(Exception ex, bool log = true)
	{
		if (log)
		{
			Logger.WriteException("App::FinalizeExceptionHandling", ex);
		}
		if (Interlocked.Exchange(ref _showingExceptionDialog, 1) != 0)
		{
			return;
		}
		Application? application = Current;
		void showFailure()
		{
			SendLog();
			if (Bootstrapper?.Dialog != null)
			{
				if (Bootstrapper.Dialog.TaskbarProgressValue == 0.0)
				{
					Bootstrapper.Dialog.TaskbarProgressValue = 1.0;
				}
				Bootstrapper.Dialog.TaskbarProgressState = TaskbarItemProgressState.Error;
			}
			Frontend.ShowExceptionDialog(ex);
		}
		try
		{
			if (application != null && !application.Dispatcher.CheckAccess())
			{
				application.Dispatcher.Invoke(showFailure);
			}
			else
			{
				showFailure();
			}
		}
		catch (Exception dialogException)
		{
			try
			{
				Logger.WriteException("App::FinalizeExceptionHandling::Dialog", dialogException);
			}
			catch
			{
			}
		}
		ShutdownApplication((int)ErrorCode.ERROR_INSTALL_FAILURE);
	}

	public static bool AllowPreReleaseUpdates => Settings?.Prop?.AllowPreReleaseUpdates == true;

	public static TimeSpan ReleaseCacheAge(bool forceRefresh) => forceRefresh ? TimeSpan.Zero : TimeSpan.FromMinutes(15L);

	public static async Task<GithubRelease?> GetLatestRelease(bool forceRefresh = false)
	{
		try
		{
			if (AllowPreReleaseUpdates)
			{
				GithubRelease? newest = await GetNewestReleaseFromListAsync(forceRefresh);
				if (newest != null)
				{
					return newest;
				}
				Logger.WriteLine("App::GetLatestRelease", "Prerelease lookup found nothing, falling back to the stable release");
			}
			GithubRelease githubRelease = await GitHubCache.GetJsonWithFallbackAsync<GithubRelease>(ProjectReleaseApi, ProjectFallbackReleaseApi, ReleaseCacheAge(forceRefresh));
			if (githubRelease == null || githubRelease.Assets == null)
			{
				Logger.WriteLine("App::GetLatestRelease", "Encountered invalid data");
				return null;
			}
			return githubRelease;
		}
		catch (Exception ex)
		{
			Logger.WriteException("App::GetLatestRelease", ex);
		}
		return null;
	}

	public static async Task<GithubRelease?> GetNewestReleaseFromListAsync(bool forceRefresh = false)
	{
		try
		{
			List<GithubRelease>? releases = await GitHubCache.GetJsonWithFallbackAsync<List<GithubRelease>>(ProjectReleaseListApi, ProjectFallbackReleaseListApi, ReleaseCacheAge(forceRefresh));
			if (releases == null)
			{
				return null;
			}
			bool allowPre = AllowPreReleaseUpdates;
			GithubRelease? best = null;
			System.Version? bestVersion = null;
			foreach (GithubRelease release in releases)
			{
				if (release == null || release.Draft || release.Assets == null)
				{
					continue;
				}
				if (release.Prerelease && !allowPre)
				{
					continue;
				}
				if (!System.Version.TryParse((release.TagName ?? "").TrimStart('v', 'V'), out System.Version? parsed))
				{
					best ??= release;
					continue;
				}
				if (bestVersion == null || parsed > bestVersion)
				{
					best = release;
					bestVersion = parsed;
				}
			}
			return best;
		}
		catch (Exception ex)
		{
			Logger.WriteException("App::GetNewestReleaseFromListAsync", ex);
			return null;
		}
	}

	public static async Task<GithubRelease?> FindReleaseByTagAsync(string tag)
	{
		if (string.IsNullOrWhiteSpace(tag))
		{
			return null;
		}
		try
		{
			List<GithubRelease>? releases = await GitHubCache.GetJsonWithFallbackAsync<List<GithubRelease>>(ProjectReleaseListApi, ProjectFallbackReleaseListApi, TimeSpan.FromMinutes(15L));
			if (releases == null)
			{
				return null;
			}
			foreach (GithubRelease release in releases)
			{
				if (release != null && !release.Draft && string.Equals(release.TagName, tag, StringComparison.OrdinalIgnoreCase))
				{
					return release;
				}
			}
			return null;
		}
		catch (Exception ex)
		{
			Logger.WriteException("App::FindReleaseByTagAsync", ex);
			return null;
		}
	}

	public static void SendStat(string _, string _1)
	{
	}

	public static void SendLog()
	{
	}

	public static void AssertWindowsOSVersion()
	{
		if (!Xenostrap.Utility.Platform.IsWindows)
		{
			return;
		}
		if (Environment.OSVersion.Version.Major < 7)
		{
			Logger.WriteLine("App::AssertWindowsOSVersion", $"Detected unsupported Windows version ({Environment.OSVersion.Version}).");
			if (!LaunchSettings.QuietFlag.Active)
			{
				Frontend.ShowMessageBox("Your Windows Version is not supported with Xenostrap!", MessageBoxImage.Hand);
			}
			Terminate(ErrorCode.ERROR_INVALID_FUNCTION);
		}
	}

	private static bool TryAutoElevateForFrameGen()
	{
		try
		{
			if (!Settings.Prop.FrameGenAutoElevate)
				return false;
			if (LaunchSettings == null || !LaunchSettings.WatcherFlag.Active)
				return false;
			if (Xenostrap.Integrations.FrameGeneration.FrameGenSettings.ModeIndex <= 0)
				return false;
			if (Xenostrap.Utility.ProcessElevation.IsAdministrator())
				return false;
			if (LaunchSettings.AdminRetriedFlag.Active)
				return false;
			Logger.WriteLine("App::OnStartup", "Restarting elevated so Frame Generation can read Roblox's real frametime");
			if (Xenostrap.Utility.ProcessElevation.TryRestartElevated(LaunchSettings.Args.Concat(["-adminretried"])))
			{
				Current?.Dispatcher.Invoke(() => Current.Shutdown());
				return true;
			}
			Logger.WriteLine("App::OnStartup", "Administrator was declined, Frame Generation continues with capture timing");
			return false;
		}
		catch (Exception ex)
		{
			Logger.WriteException("App::TryAutoElevateForFrameGen", ex);
			return false;
		}
	}

	private static readonly System.Buffers.SearchValues<char> _spaceTabQuote = System.Buffers.SearchValues.Create([' ', '\t', '"']);

	private static string BuildElevatedArgs(string[] args)
	{
		var sb = new StringBuilder();
		foreach (string a in args)
		{
			if (sb.Length > 0)
				sb.Append(' ');
			if (a.Length == 0 || a.AsSpan().IndexOfAny(_spaceTabQuote) >= 0)
				sb.Append('"').Append(a.Replace("\"", "\\\"")).Append('"');
			else
				sb.Append(a);
		}
		if (sb.Length > 0)
			sb.Append(' ');
		sb.Append("-adminretried");
		return sb.ToString();
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		RegisterExceptionHandlers();
		VpnHttpClient.Initialize();
		base.OnStartup(e);
		try
		{
			await StartAsync(e.Args);
		}
		catch (Exception ex)
		{
			FinalizeExceptionHandling(ex);
		}
	}

	private async Task StartAsync(string[] args)
	{
		TryStartup("Focus style", DisableFocusVisuals);
		TryStartup("Portable popups", EmbedPortablePopups);
		TryStartup("Portable tooltips", DisablePortableToolTips);
		TryStartup("Emoji renderer", Xenostrap.UI.EmojiTextRenderer.Install);
		TryStartup("Locale", Locale.Initialize);
		TryStartup("Icon font", Xenostrap.Utility.IconFontLoader.Install);
		TryStartup("Rounded window chrome", Xenostrap.UI.RoundedWindowChrome.Install);

		LaunchSettings = new LaunchSettings(args);
		if (LaunchSettings.DeferredCleanupFlag.Active)
		{
			await Installer.RunDeferredCleanupAsync(LaunchSettings.DeferredCleanupFlag.Data);
			Terminate();
			return;
		}
		ConfigureBuildIdentity();
		if (LaunchSettings.FactoryResetFlag.Active)
		{
			RunFactoryReset(LaunchSettings.FactoryResetFlag.Data);
			return;
		}
		bool headlessLaunch = LaunchSettings.NvApplyFlag.Active || LaunchSettings.WindowAuditFlag.Active;
		bool portableLinux = Xenostrap.Utility.Platform.IsLinux;
		string? installLocation;
		if (portableLinux)
		{
			Xenostrap.Platform.IPlatformHost? host = Xenostrap.Utility.Platform.RuntimeHost;
			if (host == null)
			{
				Frontend.ShowMessageBox("Xenostrap could not initialize Linux platform services.", MessageBoxImage.Hand);
				Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
				return;
			}

			Xenostrap.Platform.OperationResult directoryResult = await host.Paths.EnsureDirectoriesAsync(_lifetimeCancellation.Token);
			if (!directoryResult.Succeeded)
			{
				Frontend.ShowMessageBox("Xenostrap could not prepare its Linux data folders: " + (directoryResult.Failure?.Message ?? "Unknown error"), MessageBoxImage.Hand);
				Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
				return;
			}

			string? applicationPath = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(applicationPath))
			{
				Frontend.ShowMessageBox("Xenostrap could not determine its application path.", MessageBoxImage.Hand);
				Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
				return;
			}

			Paths.InitializePortable(host.Paths.Storage, applicationPath);
			installLocation = Paths.Base;
		}
		else
		{
			installLocation = InstallLocationResolver.Resolve();
			if (installLocation == null && headlessLaunch)
			{
				installLocation = Path.GetDirectoryName(Paths.Process);
			}
		}

		if (installLocation == null)
		{
			LaunchInstaller();
			return;
		}

		if (!portableLinux)
		{
			Paths.Initialize(installLocation);
		}
		if (!portableLinux && !headlessLaunch && !EnsureInstalledExecutable())
		{
			return;
		}

		Logger.Initialize(LaunchSettings.UninstallFlag.Active);
		if (!Logger.Initialized && !Logger.NoWriteMode)
		{
			Logger.WriteLine("App::OnStartup", "Possible duplicate launch detected, terminating.");
			Terminate();
			return;
		}
		LogResolvedPaths();
		if (LaunchSettings.NvApplyFlag.Active)
		{
			LaunchHandler.ProcessLaunchArgs();
			return;
		}

		if (Paths.LegacyLayoutReset && !headlessLaunch && !LaunchSettings.QuietFlag.Active && !LaunchSettings.UninstallFlag.Active && !LaunchSettings.WatcherFlag.Active)
		{
			Logger.WriteLine("App::OnStartup", "The previous install was cleared, running first time setup");
			TryStartup("Telemetry block cleanup", ClearLeftoverTelemetryBlock);
			LaunchHandler.LaunchInstaller();
			return;
		}

		Paths.EnsureDirectories();

		if (!portableLinux)
		{
			TryStartup("Cloud folder handling", PrepareCloudSyncedInstall);
			TryStartup("Install location repair", () => InstallLocationResolver.Repair(Paths.Base));
		}
		LoadPersistentState();
		TryStartup("Render acceleration", Xenostrap.Utility.RenderAcceleration.ApplyProcess);
		TryStartup("Roblox app storage", () => Xenostrap.Integrations.RobloxAppStorage.Apply());
		if (!LaunchSettings.WatcherFlag.Active)
		{
			TryStartup("AssetWarp route cleanup", () =>
			{
				Xenostrap.Integrations.AssetProxy.AssetProxyServer.CleanupStaleState();
			});
		}
		if (TryAutoElevateForFrameGen())
		{
			return;
		}
		if (LaunchSettings.WatcherFlag.Active)
		{
			InitializeWatcherServices();
			InitializeLanguage();
			LaunchHandler.ProcessLaunchArgs();
			return;
		}

		InitializeServices();
		InitializeAppearance();
		InitializeLanguage();
		if (!portableLinux && !LaunchSettings.BypassUpdateCheck)
		{
			try
			{
				await Installer.HandleUpgradeAsync();
			}
			catch (Exception ex)
			{
				Logger.WriteException("App::OnStartup::Upgrade", ex);
			}
		}
		TryStartup("API registration", WindowsRegistry.RegisterApis);
		TryStartup("Theme protocol registration", WindowsRegistry.RegisterXenostrap);
		StartInstalledThemeUpdates();
		LaunchHandler.ProcessLaunchArgs();
	}

	private static void StartInstalledThemeUpdates()
	{
		if (LaunchSettings.QuietFlag.Active || LaunchSettings.UninstallFlag.Active || LaunchSettings.WatcherFlag.Active)
		{
			return;
		}

		_ = Task.Run(async delegate
		{
			try
			{
				await Xenostrap.Integrations.BootstrapperThemes.UpdateInstalledThemesAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Logger.WriteLine("App::StartInstalledThemeUpdates", "Could not check installed themes: " + ex.Message);
			}
		});
	}

	private void RegisterExceptionHandlers()
	{
		DispatcherUnhandledException += GlobalExceptionHandler;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
#if !CROSSPLAT
		if (Xenostrap.Utility.Platform.IsWindows)
		{
			try
			{
				System.Windows.Forms.Application.ThreadException += OnWindowsFormsThreadException;
			}
			catch (Exception ex)
			{
				Logger.WriteLine("App::RegisterExceptionHandlers", "Could not hook the forms thread exception: " + ex.Message);
			}
		}
#endif
	}

	private void UnregisterExceptionHandlers()
	{
		DispatcherUnhandledException -= GlobalExceptionHandler;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
		AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
#if !CROSSPLAT
		if (Xenostrap.Utility.Platform.IsWindows)
		{
			try
			{
				System.Windows.Forms.Application.ThreadException -= OnWindowsFormsThreadException;
			}
			catch
			{
			}
		}
#endif
	}

#if !CROSSPLAT
	private void OnWindowsFormsThreadException(object? sender, System.Threading.ThreadExceptionEventArgs e)
	{
		try
		{
			Logger.WriteException("App::WindowsFormsThreadException", UnwrapException(e.Exception));
		}
		catch
		{
		}
	}
#endif

	private static bool _focusVisualsHandled;

	private static void DisableFocusVisuals()
	{
		if (_focusVisualsHandled || !Xenostrap.Utility.Platform.IsWindows)
		{
			return;
		}
		_focusVisualsHandled = true;
		if (FrameworkElement.FocusVisualStyleProperty.GetMetadata(typeof(System.Windows.Controls.Control)).DefaultValue == null)
		{
			return;
		}
		try
		{
			FrameworkElement.FocusVisualStyleProperty.OverrideMetadata(typeof(System.Windows.Controls.Control), new FrameworkPropertyMetadata(null));
		}
		catch (ArgumentException)
		{
			Logger.WriteLine("App::DisableFocusVisuals", "Focus visual metadata was already overridden, leaving it alone.");
		}
	}

	private static void DisablePortableToolTips()
	{
		if (Xenostrap.Utility.Platform.IsWindows || _portableToolTipsDisabled)
		{
			return;
		}
		EventManager.RegisterClassHandler(
			typeof(FrameworkElement),
			System.Windows.Controls.ToolTipService.ToolTipOpeningEvent,
			new System.Windows.Controls.ToolTipEventHandler(OnPortableToolTipOpening),
			true);
		_portableToolTipsDisabled = true;
	}

	private static void EmbedPortablePopups()
	{
		if (Xenostrap.Utility.Platform.IsWindows)
		{
			return;
		}
		Type? bridgeType = Type.GetType("System.Windows.Media.ProGPU.WpfPortablePopupBridge, ProGPU.Wpf", false);
		PropertyInfo? factory = bridgeType?.GetProperty("NativePopupHostFactory", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		MethodInfo? invoke = factory?.PropertyType.GetMethod("Invoke");
		if (factory == null || invoke == null)
		{
			throw new InvalidOperationException("Portable popup integration is unavailable");
		}
		ParameterExpression[] parameters = [.. invoke.GetParameters()
			.Select(parameter => LinqExpression.Parameter(parameter.ParameterType, parameter.Name))];
		Delegate embeddedFactory = LinqExpression.Lambda(factory.PropertyType, LinqExpression.Default(invoke.ReturnType), parameters).Compile();
		factory.SetValue(null, embeddedFactory);
	}

	private static void OnPortableToolTipOpening(object sender, System.Windows.Controls.ToolTipEventArgs e)
	{
		e.Handled = true;
	}

	private static BuildMetadataAttribute ResolveBuildMetadata()
	{
		BuildMetadataAttribute metadata = Assembly.GetExecutingAssembly().GetCustomAttribute<BuildMetadataAttribute>()
			?? new BuildMetadataAttribute(DateTime.UnixEpoch.ToString("o"), Environment.MachineName, "", "");
		if (metadata.Timestamp.ToUniversalTime() > DateTime.UnixEpoch)
		{
			return metadata;
		}
		try
		{
			string? executable = Environment.ProcessPath;
			if (!string.IsNullOrEmpty(executable) && File.Exists(executable))
			{
				metadata.Timestamp = File.GetLastWriteTime(executable);
			}
		}
		catch (Exception ex)
		{
			Logger.WriteLine("App::ResolveBuildMetadata", "Could not resolve the build timestamp: " + ex.Message);
		}
		return metadata;
	}

	private static void ConfigureBuildIdentity()
	{
		Logger.WriteLine("App::OnStartup", "Starting Xenostrap v" + Version);
		string userAgent = "Xenostrap/" + Version;
		if (IsActionBuild)
		{
			Logger.WriteLine("App::OnStartup", $"Compiled {BuildMetadata.Timestamp.ToFriendlyString()} from commit {BuildMetadata.CommitHash} ({BuildMetadata.CommitRef})");
			userAgent += IsProductionBuild ? " (Production)" : $" (Artifact {BuildMetadata.CommitHash}, {BuildMetadata.CommitRef})";
		}
		else
		{
			string machine = BuildMetadata.Machine ?? "";
			Logger.WriteLine("App::OnStartup", string.IsNullOrWhiteSpace(machine)
				? "Compiled " + BuildMetadata.Timestamp.ToFriendlyString()
				: "Compiled " + BuildMetadata.Timestamp.ToFriendlyString() + " from " + machine);
			userAgent += string.IsNullOrWhiteSpace(machine)
				? " (Build)"
				: " (Build " + Convert.ToBase64String(Encoding.UTF8.GetBytes(machine)) + ")";
		}
		HttpClient.DefaultRequestHeaders.Remove("User-Agent");
		HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
	}

	private static void LaunchInstaller()
	{
		Logger.Initialize(useTempDir: true);
		LaunchHandler.LaunchInstaller();
	}

	private static void RunFactoryReset(string? previousProcessId)
	{
		try
		{
			if (int.TryParse(previousProcessId, out int processId) && processId > 0 && processId != Environment.ProcessId)
			{
				using Process previousProcess = Process.GetProcessById(processId);
				if (!previousProcess.WaitForExit(15000))
				{
					throw new InvalidOperationException("The previous Xenostrap process did not close in time");
				}
			}
		}
		catch (ArgumentException)
		{
		}
		catch (Exception ex)
		{
			Logger.Initialize(useTempDir: true);
			Logger.WriteException("App::RunFactoryReset", ex);
			Frontend.ShowMessageBox("Factory reset could not start: " + ex.Message, MessageBoxImage.Hand);
			Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
			return;
		}

		try
		{
			string? installLocation = InstallLocationResolver.Resolve() ?? Path.GetDirectoryName(Paths.Process);
			if (string.IsNullOrWhiteSpace(installLocation))
			{
				LaunchInstaller();
				return;
			}

			Paths.Initialize(installLocation);
			Xenostrap.Integrations.AssetProxy.AssetProxyServer.CleanupStaleState();
			Xenostrap.Integrations.AssetProxy.AssetProxyServer.RemoveCertificates();
			if (Xenostrap.Integrations.TelemetryBlocker.IsApplied() && !Xenostrap.Integrations.TelemetryBlocker.Set(false))
			{
				throw new InvalidOperationException("The telemetry block could not be removed");
			}
			Xenostrap.Integrations.RobloxAppStorage.Reset();
			ResetGeneratedShortcuts();
			Paths.ResetUserData();
		}
		catch (Exception ex)
		{
			Logger.Initialize(useTempDir: true);
			Logger.WriteException("App::RunFactoryReset", ex);
			Frontend.ShowMessageBox("Factory reset could not finish: " + ex.Message, MessageBoxImage.Hand);
			Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
			return;
		}

		LaunchInstaller();
	}

	private static void ResetGeneratedShortcuts()
	{
		string[] shortcuts =
		[
			Path.Combine(Paths.Desktop, "Xenostrap.lnk"),
			Path.Combine(Paths.Desktop, Strings.LaunchMenu_LaunchRoblox + ".lnk"),
			Path.Combine(Paths.Desktop, Strings.LaunchMenu_LaunchRobloxStudio + ".lnk"),
			Path.Combine(Paths.Desktop, Strings.Menu_Title + ".lnk"),
			Path.Combine(Paths.WindowsStartMenu, "Xenostrap.lnk")
		];

		foreach (string shortcut in shortcuts.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			if (File.Exists(shortcut))
			{
				File.Delete(shortcut);
			}
		}
	}

	private static bool EnsureInstalledExecutable()
	{
		if (string.Equals(Paths.Process, Paths.Application, StringComparison.OrdinalIgnoreCase) || File.Exists(Paths.Application))
		{
			return true;
		}
		try
		{
			Directory.CreateDirectory(Paths.Base);
			File.Copy(Paths.Process, Paths.Application);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Initialize(useTempDir: true);
			Logger.WriteException("App::EnsureInstalledExecutable", ex);
			LaunchHandler.LaunchInstaller();
			return false;
		}
	}

	private static void ClearLeftoverTelemetryBlock()
	{
		if (!Xenostrap.Integrations.TelemetryBlocker.IsApplied())
		{
			return;
		}
		if (!Xenostrap.Utility.ProcessElevation.IsAdministrator())
		{
			Logger.WriteLine("App::OnStartup", "The hosts telemetry block is still active from the previous install, run Xenostrap as administrator to clear it");
			return;
		}
		if (Xenostrap.Integrations.TelemetryBlocker.Remove())
		{
			Settings.Prop.BlockRobloxTelemetry = false;
			Logger.WriteLine("App::OnStartup", "Cleared the hosts telemetry block left over from the previous install");
		}
	}

	private static void PrepareCloudSyncedInstall()
	{
		if (!Paths.CloudSynced)
		{
			return;
		}
		Logger.WriteLine("App::OnStartup", "The install folder is synced by a cloud provider, Roblox data is stored under " + Paths.RobloxBase);
		Xenostrap.Utility.CloudFiles.Hydrate(Paths.Application);
		Xenostrap.Utility.CloudFiles.PinInstallRoot();
	}

	private static void LogResolvedPaths()
	{
		Logger.WriteLine("App::OnStartup", "Loaded from " + Paths.Process);
		Logger.WriteLine("App::OnStartup", "Temp path is " + Paths.Temp);
		Logger.WriteLine("App::OnStartup", "WindowsStartMenu path is " + Paths.WindowsStartMenu);
	}

	private static void LoadPersistentState()
	{
		DownloadStats.Load();
		State.Load();
		RobloxState.Load();
		Settings.Load();
		FastFlags.Load(alertFailure: false);
	}

	private void InitializeServices()
	{
		TryStartup("Controller service", UI.ControllerService.Initialize);
		TryStartup("Website save queue", Xenostrap.Utility.WebsiteSaveQueue.Start);
		TryStartup("Website history sync", Xenostrap.Utility.WebsiteHistorySync.Install);
		InstallEnabledOverlays();
		TryStartup("Telemetry blocker", Xenostrap.Integrations.TelemetryBlocker.SyncSettingFromState);
		if (Xenostrap.Utility.Platform.SupportsAudioDucking)
		{
			TryStartup("Audio ducking", Xenostrap.Integrations.AudioDucker.ApplyFromSettings);
			TryStartup("Headset audio", Xenostrap.Integrations.HeadsetAudio.ApplyFromSettings);
		}
		if (!LaunchSettings.WatcherFlag.Active)
		{
			TryStartup("Rojo updater", Xenostrap.Integrations.Rojo.RojoManager.AutoUpdate);
			_ = RefreshRemoteDataAsync(_lifetimeCancellation.Token);
		}
		if (!LaunchSettings.WatcherFlag.Active && !LaunchSettings.IsHelperInvocation)
		{
			TryStartup("ORC updater", () => _ = Task.Run(async delegate
			{
				Xenostrap.Integrations.ClassicHostRedirect.CleanStaleRedirect();
				try
				{
					await Xenostrap.Utility.ClassicClients.AutoUpdateAllAsync(_lifetimeCancellation.Token).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Logger?.WriteLine("App::OrcAutoUpdate", "Auto update failed: " + ex.Message);
				}
			}));
		}
		TryStartup("CPU core limiter", CpuCoreLimiter.ApplyConfiguredLimit);
		TryStartup("GPU inventory warmup", () => Task.Run(() => _ = Xenostrap.Utility.GpuInventory.HasNvidia));
		TryStartup("Custom RPC", StartCustomRpcIfEnabled);
	}

	private static void InitializeWatcherServices()
	{
		TryStartup("Website save queue", Xenostrap.Utility.WebsiteSaveQueue.Start);
		TryStartup("Website history sync", Xenostrap.Utility.WebsiteHistorySync.Install);
		InstallEnabledOverlays();
		if (Xenostrap.Utility.Platform.SupportsAudioDucking)
		{
			TryStartup("Audio ducking", Xenostrap.Integrations.AudioDucker.ApplyFromSettings);
			TryStartup("Headset audio", Xenostrap.Integrations.HeadsetAudio.ApplyFromSettings);
		}
	}

	private static void InstallEnabledOverlays()
	{
		if (!Xenostrap.Utility.Platform.SupportsOverlays)
		{
			return;
		}
		if (Settings.Prop.RiShadeEnabled)
		{
			TryStartup("RiShade", Xenostrap.Integrations.RiShade.RiShadeManager.Install);
		}
		if (Xenostrap.Integrations.AntiAliasing.AntiAliasingSettings.MethodIndex > 0)
		{
			TryStartup("Anti aliasing", Xenostrap.Integrations.AntiAliasing.AntiAliasingManager.Install);
		}
		if (Xenostrap.Integrations.FrameGeneration.FrameGenSettings.ModeIndex > 0)
		{
			TryStartup("Frame generation", Xenostrap.Integrations.FrameGeneration.FrameGenManager.Install);
		}
		TryStartup("Overlay hub", () => Xenostrap.Integrations.Overlays.OverlayHub.Refresh());
	}

	private static void InitializeAppearance()
	{
		if (Xenostrap.Utility.Platform.IsLinux)
		{
			TryStartup("Linux scrolling", () =>
			{
				EventManager.RegisterClassHandler(typeof(System.Windows.Controls.ScrollViewer), FrameworkElement.LoadedEvent, new RoutedEventHandler(ApplyLinuxScrollViewer));
				EventManager.RegisterClassHandler(typeof(System.Windows.Controls.Image), FrameworkElement.LoadedEvent, new RoutedEventHandler(ApplyLinuxImageScaling));
			});
		}
		if (Settings.Prop.ClearFont)
		{
			TryStartup("Clear font", () => EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(ApplyClearFont)));
		}
		TryStartup("Smooth scrolling", () =>
		{
			Wpf.Ui.Controls.SmoothScroll.SetGlobalEnabled(Settings.Prop.SmooothBARRyesirikikthxlucipook);
			Wpf.Ui.Controls.SmoothScroll.Register();
		});
		TryStartup("Global background", GlobalBackground.Register);
		TryStartup("Application font", AppFont.Initialize);
		TryStartup("Memory manager", Xenostrap.Utility.MemoryManager.Start);
		TryStartup("Render diagnostics", LogRenderMode);
	}

	private static void ApplyClearFont(object sender, RoutedEventArgs e)
	{
		if (sender is not Window window)
		{
			return;
		}
		TextOptions.SetTextRenderingMode(window, TextRenderingMode.ClearType);
		TextOptions.SetTextFormattingMode(window, TextFormattingMode.Display);
		window.UseLayoutRounding = true;
		window.SnapsToDevicePixels = true;
		RenderOptions.SetClearTypeHint(window, ClearTypeHint.Enabled);
	}

	private static void LogRenderMode()
	{
		int renderTier = RenderCapability.Tier >> 16;
		bool environmentSoftware = Environment.GetEnvironmentVariable("LIBGL_ALWAYS_SOFTWARE") == "1";
		string renderMode = Settings.Prop.WPFSoftwareRender || LaunchSettings.NoGPUFlag.Active || environmentSoftware ? "software" : renderTier == 0 ? "software, GPU tier 0" : "hardware";
		Logger.WriteLine("App::OnStartup", $"WPF render tier {renderTier}, rendering mode: {renderMode}");
		if (Xenostrap.Utility.Platform.IsLinux)
		{
			Logger.WriteLine("App::OnStartup", "Linux session: " + (Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "unknown"));
		}
	}

	private static void ApplyLinuxScrollViewer(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.ScrollViewer viewer)
		{
			System.Windows.Controls.ScrollViewer.SetIsDeferredScrollingEnabled(viewer, true);
			RenderOptions.SetBitmapScalingMode(viewer, BitmapScalingMode.Linear);
		}
	}

	private static void ApplyLinuxImageScaling(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Image image)
			RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Linear);
	}

	private static void InitializeLanguage()
	{
		if (!Locale.SupportedLocales.ContainsKey(Settings.Prop.Locale))
		{
			Settings.Prop.Locale = Locale.DefaultLocale;
			Settings.Save();
		}
		Locale.Set(Settings.Prop.Locale);
		TryStartup("Resource proxy", ResourceProxy.Inject);
		TryStartup("Translation service", TranslationService.Initialize);
		TryStartup("Language refresher", Xenostrap.UI.LiveLanguageRefresher.Initialize);
	}

	private static async Task RefreshRemoteDataAsync(CancellationToken cancellationToken)
	{
		try
		{
			await Xenostrap.Utility.WebsiteGeoSync.PullAsync().ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception ex)
		{
			Logger.WriteException("App::RefreshRemoteData", ex);
		}
	}

	private static void StartCustomRpcIfEnabled()
	{
		string configPath = Path.Combine(Paths.UserData, "discord-rpc.json");
		if (!File.Exists(configPath))
		{
			return;
		}
		if (JsonFile.TryLoad<JsonElement>(configPath, JsonOptions.Tolerant, out JsonElement config, out bool recovered, out Exception? failure, 4194304) && config.TryGetProperty("AutoStartRpc", out JsonElement autoStart) && autoStart.ValueKind == JsonValueKind.True)
		{
			_ = RPCCustomizerViewModel.Shared;
		}
		if (recovered)
			Logger.WriteLine("App::StartCustomRpcIfEnabled", "Recovered the last valid RPC configuration backup");
		if (failure != null && config.ValueKind == JsonValueKind.Undefined)
			Logger.WriteLine("App::StartCustomRpcIfEnabled", "RPC configuration is invalid: " + failure.Message);
	}

	private static void TryStartup(string component, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Logger.WriteException("App::OnStartup::" + component, ex);
		}
	}


	protected override void OnExit(ExitEventArgs e)
	{
		UnregisterExceptionHandlers();
		TryShutdown(VpnHttpClient.Shutdown);
		TryShutdown(_lifetimeCancellation.Cancel);
		TryShutdown(Xenostrap.Utility.ScreenColorEffect.Shutdown);
		TryShutdown(Xenostrap.UI.EmojiTextRenderer.Shutdown);
		TryShutdown(Xenostrap.Utility.WebsiteSaveQueue.Shutdown);
		TryShutdown(Xenostrap.Utility.WebsiteHistorySync.Shutdown);
		TryShutdown(Xenostrap.Utility.WebsiteGeoSync.Shutdown);
		TryShutdown(Xenostrap.Integrations.ServerFetchStore.Shutdown);
		TryShutdown(Xenostrap.Utility.MemoryManager.Shutdown);
		TryShutdown(Xenostrap.Utility.LinuxStartup.Shutdown);
		TryShutdown(Xenostrap.UI.ControllerService.Shutdown);
		TryShutdown(Xenostrap.Utility.ClassicIntegrations.Stop);
		TryShutdown(Xenostrap.Integrations.ClassicHostRedirect.RemoveIfStale);
		TryShutdown(Xenostrap.Integrations.RiShade.RiShadeManager.Shutdown);
		TryShutdown(Xenostrap.Integrations.AntiAliasing.AntiAliasingManager.Shutdown);
		TryShutdown(Xenostrap.Integrations.FrameGeneration.FrameGenManager.Shutdown);
		TryShutdown(Xenostrap.Integrations.HeadsetAudio.Shutdown);
		TryShutdown(Xenostrap.Integrations.AudioDucker.Shutdown);
		TryShutdown(Xenostrap.Integrations.Rojo.RojoManager.Shutdown);
		TryShutdown(Xenostrap.Integrations.Studio.StudioIntegration.Shutdown);
		TryShutdown(AssetProxyServer.Stop);
		TryShutdown(AssetPreloadCache.Shutdown);
		TryShutdown(AssetCaptureStore.Shutdown);
		TryShutdown(Xenostrap.Integrations.Fullscreen.FakeExclusiveFullscreen.Shutdown);
		TryShutdown(Xenostrap.Integrations.Overlays.RobloxWindowTracker.Shutdown);
		TryShutdown(Xenostrap.UI.LiveLanguageRefresher.Shutdown);
		TryShutdown(Xenostrap.UI.Utility.WindowScaling.Shutdown);
		TryShutdown(Xenostrap.Utility.TranslationService.Shutdown);
		TryShutdown(Xenostrap.UI.GlobalBackground.ClearCache);
		TryShutdown(Xenostrap.Utility.DynamicRenderSystem.ClearCache);
		TryShutdown(Settings.FlushDeferred);
		TryShutdown(FastFlags.FlushDeferred);
		TryShutdown(StopCustomRpc);
		TryShutdown(DisposeDiscordClient);
		TryShutdown(DisposeMusicPlayer);
		TryShutdown(_httpClient.Dispose);
		TryShutdown(_lifetimeCancellation.Dispose);
		try
		{
			Logger.Dispose();
		}
		catch
		{
		}
		base.OnExit(e);
	}

	private static void TryShutdown(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			try
			{
				Logger.WriteException("App::OnExit", ex);
			}
			catch
			{
			}
		}
	}

	private static void StopCustomRpc()
	{
		RPCCustomizerViewModel.SharedOrNull?.Dispose();
	}

	private static void DisposeDiscordClient()
	{
		DiscordClient?.Dispose();
		DiscordClient = null;
	}

	private static void DisposeMusicPlayer()
	{
		foreach (Window window in Application.Current.Windows)
		{
			if (window is MusicPlayer musicPlayer)
			{
				musicPlayer.Shutdown();
			}
		}
	}
}
