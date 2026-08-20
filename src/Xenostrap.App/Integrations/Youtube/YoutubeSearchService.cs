using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Xenostrap.Integrations.Youtube;

public static class YoutubeSearchService
{
    private const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private const int MaxResultsPageBytes = 8 * 1024 * 1024;
    private const int DefaultMaxResults = 40;
    private const int MaxDepth = 48;

    public static async Task<IReadOnlyList<YoutubeSearchResult>> SearchAsync(string query, int maxResults = DefaultMaxResults, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<YoutubeSearchResult>();

        string url = "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query.Trim());
        using var client = Xenostrap.Utility.VpnHttpClient.Create(TimeSpan.FromSeconds(20));
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", ChromeUserAgent);
        request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Cookie", "CONSENT=YES+cb; SOCS=CAI");

        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string html = await Xenostrap.Utility.Http.ReadStringBoundedAsync(response.Content, MaxResultsPageBytes, token).ConfigureAwait(false);

        string? json = ExtractInitialData(html);
        if (json == null)
            throw new InvalidOperationException("Could not read YouTube search results.");

        var results = new List<YoutubeSearchResult>();
        using (JsonDocument document = JsonDocument.Parse(json))
            CollectVideos(document.RootElement, results, Math.Max(1, maxResults), 0);
        return results;
    }

    private static string? ExtractInitialData(string html)
    {
        const string marker = "var ytInitialData = ";
        const string altMarker = "window[\"ytInitialData\"] = ";
        int start = html.IndexOf(marker, StringComparison.Ordinal);
        int offset = marker.Length;
        if (start < 0)
        {
            start = html.IndexOf(altMarker, StringComparison.Ordinal);
            offset = altMarker.Length;
        }
        if (start < 0)
            return null;

        int brace = html.IndexOf('{', start + offset);
        if (brace < 0)
            return null;

        bool inString = false;
        bool escaped = false;
        int depth = 0;
        for (int i = brace; i < html.Length; i++)
        {
            char c = html[i];
            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inString = false;
                continue;
            }
            if (c == '"')
                inString = true;
            else if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return html.Substring(brace, i - brace + 1);
            }
        }
        return null;
    }

    private static void CollectVideos(JsonElement element, List<YoutubeSearchResult> results, int maxResults, int depth)
    {
        if (results.Count >= maxResults || depth > MaxDepth)
            return;
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("videoRenderer", out JsonElement video) && video.ValueKind == JsonValueKind.Object)
                {
                    YoutubeSearchResult? parsed = TryParseVideo(video);
                    if (parsed != null)
                        results.Add(parsed);
                    return;
                }
                foreach (JsonProperty property in element.EnumerateObject())
                    CollectVideos(property.Value, results, maxResults, depth + 1);
                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                    CollectVideos(item, results, maxResults, depth + 1);
                break;
        }
    }

    private static YoutubeSearchResult? TryParseVideo(JsonElement video)
    {
        string videoId = GetText(video, "videoId");
        if (videoId.Length == 0)
            return null;

        string title = GetText(video, "title").Trim();
        if (title.Length == 0)
            return null;

        string channel = GetText(video, "ownerText").Trim();
        if (channel.Length == 0)
            channel = GetText(video, "longBylineText").Trim();

        string durationText = GetText(video, "lengthText").Trim();
        return new YoutubeSearchResult
        {
            VideoId = videoId,
            Title = title,
            Channel = channel,
            DurationText = durationText,
            DurationSeconds = ParseDurationSeconds(durationText)
        };
    }

    private static string GetText(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement property))
            return string.Empty;
        switch (property.ValueKind)
        {
            case JsonValueKind.String:
                return property.GetString() ?? string.Empty;
            case JsonValueKind.Object:
                if (property.TryGetProperty("simpleText", out JsonElement simple) && simple.ValueKind == JsonValueKind.String)
                    return simple.GetString() ?? string.Empty;
                if (property.TryGetProperty("runs", out JsonElement runs) && runs.ValueKind == JsonValueKind.Array)
                {
                    var builder = new StringBuilder();
                    foreach (JsonElement run in runs.EnumerateArray())
                    {
                        if (run.TryGetProperty("text", out JsonElement text) && text.ValueKind == JsonValueKind.String)
                            builder.Append(text.GetString());
                    }
                    return builder.ToString();
                }
                break;
        }
        return string.Empty;
    }

    private static int ParseDurationSeconds(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        int total = 0;
        foreach (string part in text.Split(':'))
        {
            if (!int.TryParse(part, out int value))
                return 0;
            total = total * 60 + value;
        }
        return total;
    }
}