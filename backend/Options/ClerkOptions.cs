namespace Leaderboard.Options;

/// <summary>
/// Represents configuration options for Clerk authentication.
/// </summary>
public sealed class ClerkOptions
{
	/// <summary>
	/// Gets the name of the configuration section for Clerk options.
	/// </summary>
	public const string SectionName = "Clerk";
	/// <summary>
	/// Gets or sets the Clerk API key used for authentication.
	/// </summary>
	public string Issuer { get; set; } = string.Empty;
}