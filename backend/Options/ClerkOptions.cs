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
	/// Gets or sets the Clerk issuer used for token validation.
	/// </summary>
	public string Issuer { get; set; } = string.Empty;
	/// <summary>
	/// Gets or sets the Clerk secret key used for server-side directory lookups.
	/// </summary>
	public string SecretKey { get; set; } = string.Empty;
	/// <summary>
	/// Gets or sets the Clerk webhook signing secret.
	/// </summary>
	public string WebhookSecret { get; set; } = string.Empty;
}