using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Xenostrap.Integrations.Youtube;

public static class YoutubeToolchain
{
    private const string LogTag = "Youtube";
    private const long MaxYtDlpBytes = 64L * 1024 * 1024;
    private const long MaxFfmpegZipBytes = 256L * 1024 * 1024;

    private static readonly HttpClient Http = CreateClient();
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static readonly Regex DownloadProgressRegex = new(@"\[download\]\s+(\d+(?:\.\d+)?)\s*%", RegexOptions.Compiled);

    public static string ToolchainDir => Path.Combine(Paths.Cache, "Youtube");

    public static string YtDlpExe => Path.Combine(ToolchainDir, "yt-dlp.exe");

    public static string FfmpegExe => Path.Combine(ToolchainDir, "ffmpeg.exe");

    private static HttpClient CreateClient()
    {
        var http = Xenostrap.Utility.VpnHttpClient.Create(TimeSpan.FromMinutes(30));
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Xenostrap");
        return http;
    }

    public static async Task<string> DownloadAudioAsync(string url, string outputDirectory, string baseName, Action<string, double, bool>? progress, CancellationToken token)
    {
        string ytDlp = await EnsureYtDlpAsync(progress, token).ConfigureAwait(false);
        string? ffmpeg = null;
        try
        {
            ffmpeg = await EnsureFfmpegAsync(progress, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            App.Logger.WriteLine(LogTag, "ffmpeg unavailable, using m4a-only fallback: " + ex.Message);
        }

        Directory.CreateDirectory(outputDirectory);
        string outputTemplate = Path.Combine(outputDirectory, baseName + ".%(ext)s");

        var arguments = new List<string>
        {
            "--no-playlist",
            "--no-part",
            "--no-mtime",
            "--newline",
            "--progress",
            "-o",
            outputTemplate
        };
        if (ffmpeg != null)
        {
            arguments.Add("--ffmpeg-location");
            arguments.Add(Path.GetDirectoryName(ffmpeg)!);
            arguments.Add("-f");
            arguments.Add("bestaudio");
            arguments.Add("-x");
            arguments.Add("--audio-format");
            arguments.Add("m4a");
        }
        else
        {
            arguments.Add("-f");
            arguments.Add("bestaudio[ext=m4a]");
        }
        arguments.Add(url);

        progress?.Invoke("Downloading audio...", -1.0, true);

        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlp,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start yt-dlp.");
        using (token.Register(() =>
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }
        }))
        {
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(token);
            Task<Queue<string>> stderrTask = ReadErrorTailAsync(process.StandardError, progress, token);
            await process.WaitForExitAsync(token).ConfigureAwait(false);
            int exitCode = process.ExitCode;
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

            if (exitCode != 0)
            {
                Queue<string> tail = stderrTask.Result;
                string detail = string.Join(" ", tail.Where(line => !string.IsNullOrWhiteSpace(line)).TakeLast(3));
                throw new InvalidOperationException("yt-dlp exited with code " + exitCode + (detail.Length > 0 ? ": " + detail : ""));
            }
        }

        string[] candidates = Directory.GetFiles(outputDirectory, baseName + ".*", SearchOption.TopDirectoryOnly);
        string? file = candidates.FirstOrDefault(candidate => !candidate.EndsWith(".part", StringComparison.OrdinalIgnoreCase));
        if (file == null)
            throw new InvalidOperationException("yt-dlp did not produce an audio file.");

        App.Logger.WriteLine(LogTag, "Downloaded " + file);
        return file;
    }

    public static void PreinstallAsync()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                await EnsureYtDlpAsync(null, cts.Token).ConfigureAwait(false);
                await EnsureFfmpegAsync(null, cts.Token).ConfigureAwait(false);
                App.Logger.WriteLine(LogTag, "Toolchain preinstalled into " + ToolchainDir);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogTag, "Toolchain preinstall failed (will download on demand): " + ex.Message);
            }
        });
    }

    public static async Task<string> EnsureYtDlpAsync(Action<string, double, bool>? progress, CancellationToken token)
    {
        string? onPath = FindOnPath("yt-dlp");
        if (onPath != null)
            return onPath;
        if (File.Exists(YtDlpExe))
            return YtDlpExe;

        await Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (File.Exists(YtDlpExe))
                return YtDlpExe;
            Directory.CreateDirectory(ToolchainDir);
            const string url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
            await Xenostrap.Utility.ResilientDownload.DownloadAsync(Http, [url], YtDlpExe, MaxYtDlpBytes, token,
                progress: (read, total) =>
                {
                    double fraction = total is > 0 ? (double)read / total.Value : -1.0;
                    progress?.Invoke(fraction >= 0 ? $"Downloading yt-dlp {fraction * 100:0}%" : "Downloading yt-dlp...", fraction, true);
                }).ConfigureAwait(false);
            App.Logger.WriteLine(LogTag, "yt-dlp installed at " + YtDlpExe);
            return YtDlpExe;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<string> EnsureFfmpegAsync(Action<string, double, bool>? progress, CancellationToken token)
    {
        string? onPath = FindOnPath("ffmpeg");
        if (onPath != null)
            return onPath;
        if (File.Exists(FfmpegExe))
            return FfmpegExe;

        await Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (File.Exists(FfmpegExe))
                return FfmpegExe;
            Directory.CreateDirectory(ToolchainDir);
            string zipPath = Path.Combine(ToolchainDir, "ffmpeg.zip");
            try
            {
                const string url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
                await Xenostrap.Utility.ResilientDownload.DownloadAsync(Http, [url], zipPath, MaxFfmpegZipBytes, token,
                    progress: (read, total) =>
                    {
                        double fraction = total is > 0 ? (double)read / total.Value : -1.0;
                        progress?.Invoke(fraction >= 0 ? $"Downloading ffmpeg {fraction * 100:0}%" : "Downloading ffmpeg...", fraction, true);
                    }).ConfigureAwait(false);
                progress?.Invoke("Extracting ffmpeg...", -1.0, true);
                ExtractFfmpeg(zipPath, FfmpegExe);
            }
            finally
            {
                TryDelete(zipPath);
            }
            App.Logger.WriteLine(LogTag, "ffmpeg installed at " + FfmpegExe);
            return FfmpegExe;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static void ExtractFfmpeg(string zipPath, string destination)
    {
        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(item =>
            item.FullName.EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            throw new InvalidOperationException("ffmpeg.exe missing from the archive.");
        if (entry.Length <= 0 || entry.Length > MaxFfmpegZipBytes)
            throw new InvalidDataException("ffmpeg.exe has an invalid size.");
        entry.ExtractToFile(destination, overwrite: true);
    }

    private static string? FindOnPath(string name)
    {
        string pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathValue))
            return null;
        string[] extensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                continue;
            foreach (string extension in extensions)
            {
                string candidate = Path.Combine(directory, name + extension);
                if (File.Exists(candidate))
                    return candidate;
            }
            string plain = Path.Combine(directory, name);
            if (File.Exists(plain))
                return plain;
        }
        return null;
    }

    private static async Task<Queue<string>> ReadErrorTailAsync(StreamReader reader, Action<string, double, bool>? progress, CancellationToken token)
    {
        var tail = new Queue<string>();
        string? line;
        while ((line = await reader.ReadLineAsync(token).ConfigureAwait(false)) != null)
        {
            if (line.Length == 0)
                continue;
            Match match = DownloadProgressRegex.Match(line);
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
                progress?.Invoke("Downloading audio...", Math.Clamp(percent / 100.0, 0.0, 1.0), true);
            else if (line[0] == '[')
            {
                int close = line.IndexOf(']');
                progress?.Invoke(close > 0 ? line[(close + 1)..].Trim() : line, -1.0, true);
            }
            tail.Enqueue(line);
            while (tail.Count > 10)
                tail.Dequeue();
        }
        return tail;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}