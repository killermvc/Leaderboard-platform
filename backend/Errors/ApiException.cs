using Leaderboard.Errors;

namespace Leaderboard.Exceptions;

public abstract class ApiException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    public object? Details { get; }

    protected ApiException(string errorCode, string message, int statusCode, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 404, details) { }
}

public class ValidationException : ApiException
{
    public ValidationException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 400, details) { }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 401, details) { }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 403, details) { }
}

public class ConflictException : ApiException
{
    public ConflictException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 409, details) { }
}

public class RateLimitException : ApiException
{
    public RateLimitException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 429, details) { }
}

public class ServiceUnavailableException : ApiException
{
    public ServiceUnavailableException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 503, details) { }
}