using System.Text.Json.Serialization;

namespace Leaderboard.Errors;

public class ErrorResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public object? Details { get; set; }

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