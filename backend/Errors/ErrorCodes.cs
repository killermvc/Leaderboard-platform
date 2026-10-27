namespace Leaderboard.Errors;

/// <summary>
/// Static class containing error codes used throughout the application. Each constant represents a specific error condition that can occur, allowing for consistent error handling and messaging across the application.
/// </summary>
public static class ErrorCodes
{
    // Authentication & Authorization
	/// <summary>Represents an error code for unauthorized access.</summary>
    public const string Unauthorized = "UNAUTHORIZED";
    /// <summary>Represents an error code for forbidden access.</summary>
    public const string Forbidden = "FORBIDDEN";
    /// <summary>Represents an error code for an invalid authentication token.</summary>
    public const string InvalidToken = "INVALID_TOKEN";
    /// <summary>Represents an error code for an expired authentication token.</summary>
    public const string TokenExpired = "TOKEN_EXPIRED";

    // Validation
    /// <summary>Represents an error code for a failed validation.</summary>
    public const string ValidationFailed = "VALIDATION_FAILED";
    /// <summary>Represents an error code for an invalid request.</summary>
    public const string InvalidRequest = "INVALID_REQUEST";

    // Resource Not Found
    /// <summary>Represents an error code for a resource that was not found.</summary>
    public const string NotFound = "NOT_FOUND";
    /// <summary>Represents an error code for a game that was not found.</summary>
    public const string GameNotFound = "GAME_NOT_FOUND";
    /// <summary>Represents an error code for a score that was not found.</summary>
    public const string ScoreNotFound = "SCORE_NOT_FOUND";
    /// <summary>Represents an error code for a user that was not found.</summary>
    public const string UserNotFound = "USER_NOT_FOUND";
    /// <summary>Represents an error code for an API key that was not found.</summary>
    public const string ApiKeyNotFound = "API_KEY_NOT_FOUND";

    // Business Logic
    /// <summary>Represents an error code for disallowed submissions.</summary>
    public const string SubmissionsNotAllowed = "SUBMISSIONS_NOT_ALLOWED";
    /// <summary>Represents an error code for a score that is not higher than the existing score.</summary>
    public const string ScoreNotHigher = "SCORE_NOT_HIGHER";
    /// <summary>Represents an error code for a duplicate submission.</summary>
    public const string DuplicateSubmission = "DUPLICATE_SUBMISSION";
    /// <summary>Represents an error code for attempting to moderate one's own score.</summary>
    public const string CannotModerateOwnScore = "CANNOT_MODERATE_OWN_SCORE";
    /// <summary>Represents an error code for a user who is already a moderator.</summary>
    public const string AlreadyModerator = "ALREADY_MODERATOR";
    /// <summary>Represents an error code for a user who is not a moderator.</summary>
    public const string NotModerator = "NOT_MODERATOR";
    /// <summary>Represents an error code for attempting to remove the owner.</summary>
    public const string CannotRemoveOwner = "CANNOT_REMOVE_OWNER";
    /// <summary>Represents an error code for a game that already exists.</summary>
    public const string GameAlreadyExists = "GAME_ALREADY_EXISTS";

    // Rate Limiting
    /// <summary>Represents an error code for an exceeded rate limit.</summary>
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

    // Server Errors
    /// <summary>Represents an error code for an internal server error.</summary>
    public const string InternalError = "INTERNAL_ERROR";
    /// <summary>Represents an error code for an unavailable service.</summary>
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    /// <summary>Represents an error code for a database error.</summary>
    public const string DatabaseError = "DATABASE_ERROR";
    /// <summary>Represents an error code for an external service error.</summary>
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";

    // Clerk/Webhook
    /// <summary>Represents an error code for a Clerk error.</summary>
    public const string ClerkError = "CLERK_ERROR";
    /// <summary>Represents an error code for failed webhook verification.</summary>
    public const string WebhookVerificationFailed = "WEBHOOK_VERIFICATION_FAILED";
}