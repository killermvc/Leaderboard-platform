namespace Leaderboard.Errors;

public static class ErrorCodes
{
    // Authentication & Authorization
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InvalidToken = "INVALID_TOKEN";
    public const string TokenExpired = "TOKEN_EXPIRED";

    // Validation
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidRequest = "INVALID_REQUEST";

    // Resource Not Found
    public const string NotFound = "NOT_FOUND";
    public const string GameNotFound = "GAME_NOT_FOUND";
    public const string ScoreNotFound = "SCORE_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string ApiKeyNotFound = "API_KEY_NOT_FOUND";

    // Business Logic
    public const string SubmissionsNotAllowed = "SUBMISSIONS_NOT_ALLOWED";
    public const string ScoreNotHigher = "SCORE_NOT_HIGHER";
    public const string DuplicateSubmission = "DUPLICATE_SUBMISSION";
    public const string CannotModerateOwnScore = "CANNOT_MODERATE_OWN_SCORE";
    public const string AlreadyModerator = "ALREADY_MODERATOR";
    public const string NotModerator = "NOT_MODERATOR";
    public const string CannotRemoveOwner = "CANNOT_REMOVE_OWNER";
    public const string GameAlreadyExists = "GAME_ALREADY_EXISTS";

    // Rate Limiting
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

    // Server Errors
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";

    // Clerk/Webhook
    public const string ClerkError = "CLERK_ERROR";
    public const string WebhookVerificationFailed = "WEBHOOK_VERIFICATION_FAILED";
}