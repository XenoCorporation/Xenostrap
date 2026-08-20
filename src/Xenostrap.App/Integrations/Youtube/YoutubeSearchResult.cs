namespace Xenostrap.Integrations.Youtube;

public sealed class YoutubeSearchResult
{
    public string VideoId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Channel { get; init; } = string.Empty;

    public string DurationText { get; init; } = string.Empty;

    public int DurationSeconds { get; init; }

    public string Url => "https://www.youtube.com/watch?v=" + VideoId;

    public string ThumbnailUrl => "https://i.ytimg.com/vi/" + VideoId + "/hqdefault.jpg";
}