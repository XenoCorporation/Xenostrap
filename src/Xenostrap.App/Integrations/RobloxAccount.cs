namespace Xenostrap.Integrations;

public sealed class RobloxAccount
{
	public long UserId { get; init; }

	public string Username { get; init; } = "";

	public string DisplayName { get; init; } = "";
}
