using System.Text.Json.Serialization;

namespace Leaderboard.Errors;

/// <summary>
/// Represents a standardized error response structure used in the application. This class encapsulates the error code, message, and optional details that can be returned to clients when an error occurs. It provides a consistent format for error responses, making it easier for clients to handle and interpret errors.
/// </summary>
public class ErrorResponse
{
	/// <summary>
	/// Gets or sets the error code associated with the error response. This code can be used to identify the specific error condition that occurred.
	/// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the message describing the error. This message provides a human-readable explanation of the error condition and can be used to inform clients about what went wrong.
	/// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets additional details about the error. This property can be used to provide more context about the error condition, such as validation errors or other relevant information. It is optional and may be null if no additional details are provided.
	/// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; set; }

	/// <summary>
	/// Creates a new instance of the <see cref="ErrorResponse"/> class with the specified error code, message, and optional details. This static method provides a convenient way to create error responses in a consistent format.
	/// </summary>
	/// <param name="code">The error code to set.</param>
	/// <param name="message">The message to set.</param>
	/// <param name="details">The details to set.</param>
	/// <returns>A new instance of the <see cref="ErrorResponse"/> class.</returns>
    public static ErrorResponse From(string code, string message, object? details = null)
    {
        return new ErrorResponse
        {
            Code = code,
            Message = message,
            Details = details
        };
    }
}