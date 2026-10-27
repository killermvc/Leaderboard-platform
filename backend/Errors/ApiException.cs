using Leaderboard.Errors;

namespace Leaderboard.Exceptions;

/// <summary>
/// Base class for API exceptions. This class provides a structure for defining custom exceptions that can be thrown in the application to represent various error conditions. Each exception includes an error code, a message, an HTTP status code, and optional additional details.
/// </summary>
public abstract class ApiException : Exception
{
	/// <summary>
	/// Gets the error code associated with the exception. This code can be used to identify the specific error condition that occurred.
	/// </summary>
    public string ErrorCode { get; }
	/// <summary>
	/// Gets the HTTP status code associated with the exception. This code indicates the type of error that occurred and can be used to determine the appropriate response to return to the client.
	/// </summary>
    public int StatusCode { get; }
	/// <summary>
	/// Gets additional details about the exception. This property can be used to provide more context about the error condition, such as validation errors or other relevant information. It is optional and may be null if no additional details are provided.
	/// </summary>
    public object? Details { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ApiException"/> class with the specified error code, message, HTTP status code, and optional details. This constructor is protected to allow derived classes to specify their own error codes and messages while still providing a consistent structure for API exceptions.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="statusCode">The HTTP status code associated with the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    protected ApiException(string errorCode, string message, int statusCode, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details;
    }
}

/// <summary>
/// Represents a "Not Found" error condition. This exception is thrown when a requested resource cannot be found. It includes an error code, a message, and an optional details object. The HTTP status code for this exception is 404.
/// </summary>
public class NotFoundException : ApiException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NotFoundException"/> class with the specified error code, message, and optional details. This constructor sets the HTTP status code to 404 to indicate that the requested resource was not found.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    public NotFoundException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 404, details) { }
}

/// <summary>
/// Represents a "Validation" error condition. This exception is thrown when input validation fails. It includes an error code, a message, and an optional details object. The HTTP status code for this exception is 400.
/// </summary>
public class ValidationException : ApiException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationException"/> class with the specified error code, message, and optional details. This constructor sets the HTTP status code to 400 to indicate that the request was invalid due to validation errors.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    public ValidationException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 400, details) { }
}

/// <summary>
/// Represents an "Unauthorized" error condition. This exception is thrown when a user attempts to access a resource without proper authentication. It includes an error code, a message, and an optional details object. The HTTP status code for this exception is 401.
/// </summary>
public class UnauthorizedException : ApiException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UnauthorizedException"/> class with the specified error code, message, and optional details. This constructor sets the HTTP status code to 401 to indicate that the request requires user authentication.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    public UnauthorizedException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 401, details) { }
}

/// <summary>
/// Represents a "Forbidden" error condition. This exception is thrown when a user attempts to access a resource they do not have permission to access. It includes an error code, a message, and an optional details object. The HTTP status code for this exception is 403.
/// </summary>
public class ForbiddenException : ApiException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ForbiddenException"/> class with the specified error code, message, and optional details. This constructor sets the HTTP status code to 403 to indicate that the server understood the request but refuses to authorize it.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    public ForbiddenException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 403, details) { }
}

/// <summary>
/// Represents a "Conflict" error condition. This exception is thrown when a request conflicts with the current state of the resource. It includes an error code, a message, and an optional details object. The HTTP status code for this exception is 409.
/// </summary>
public class ConflictException : ApiException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConflictException"/> class with the specified error code, message, and optional details. This constructor sets the HTTP status code to 409 to indicate that the request could not be completed due to a conflict with the current state of the resource.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    public ConflictException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 409, details) { }
}

/// <summary>
/// Represents a "Rate Limit Exceeded" error condition. This exception is thrown when a user exceeds the allowed number of requests in a given time frame. It includes an error code, a message, and an optional details object. The HTTP status code for this exception is 429.
/// </summary>
public class RateLimitException : ApiException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RateLimitException"/> class with the specified error code, message, and optional details. This constructor sets the HTTP status code to 429 to indicate that the user has sent too many requests in a given amount of time.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    public RateLimitException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 429, details) { }
}

/// <summary>
/// Represents a "Service Unavailable" error condition. This exception is thrown when the server is temporarily unable to handle the request, often due to maintenance or overload. It includes an error code, a message, and an optional details object. The HTTP status code for this exception is 503.
/// </summary>
public class ServiceUnavailableException : ApiException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class with the specified error code, message, and optional details. This constructor sets the HTTP status code to 503 to indicate that the server is currently unable to handle the request due to temporary overloading or maintenance.
	/// </summary>
	/// <param name="errorCode">The error code associated with the exception.</param>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="details">Optional additional details about the exception.</param>
    public ServiceUnavailableException(string errorCode, string message, object? details = null)
        : base(errorCode, message, 503, details) { }
}