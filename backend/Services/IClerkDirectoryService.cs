namespace Leaderboard.Services;

#pragma warning disable CS1591

public interface IClerkDirectoryService
{
	Task<IReadOnlyList<ClerkDirectoryUser>> SearchUsersAsync(string query, int limit, CancellationToken cancellationToken = default);
	Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(IReadOnlyCollection<string> clerkUserIds, CancellationToken cancellationToken = default);
	Task<string> GetDisplayNameAsync(string clerkUserId, CancellationToken cancellationToken = default);
}

public sealed record ClerkDirectoryUser(string ClerkUserId, string DisplayName);

#pragma warning restore CS1591
