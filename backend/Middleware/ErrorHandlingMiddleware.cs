using System.Text.Json;
using Leaderboard.Errors;
using Leaderboard.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Leaderboard.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await HandleApiExceptionAsync(context, ex);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            await HandleInvalidOperationExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedAccessExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedExceptionAsync(context, ex);
        }
    }

    private static Task HandleApiExceptionAsync(HttpContext context, ApiException ex)
    {
        var response = ErrorResponse.From(ex.ErrorCode, ex.Message, ex.Details);
        context.Response.StatusCode = ex.StatusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException ex)
    {
        var response = ErrorResponse.From(ErrorCodes.NotFound, ex.Message);
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static Task HandleInvalidOperationExceptionAsync(HttpContext context, InvalidOperationException ex)
    {
        var response = ErrorResponse.From(ErrorCodes.ValidationFailed, ex.Message);
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static Task HandleUnauthorizedAccessExceptionAsync(HttpContext context, UnauthorizedAccessException ex)
    {
        var response = ErrorResponse.From(ErrorCodes.Forbidden, ex.Message);
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private Task HandleUnexpectedExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception occurred");

        var response = ErrorResponse.From(ErrorCodes.InternalError, "An unexpected error occurred.");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}