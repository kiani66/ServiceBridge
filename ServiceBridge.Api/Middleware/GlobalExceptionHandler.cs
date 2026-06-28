using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Log with full details server-side only
        _logger.LogError(exception,
            "Unhandled exception on {Method} {Path} | CorrelationId: {CorrelationId} | Message: {Message}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier,
            exception.Message);

        var statusCode = HttpStatusCode.InternalServerError;
        var errorCode = AppConstants.ErrorCodes.UnexpectedError;
        var message = AppMessages.UnexpectedError;

        // Handle specific exception types
        switch (exception)
        {
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                errorCode = AppConstants.ErrorCodes.Unauthorized;
                message = AppMessages.UnauthorizedAccess;
                break;

            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                errorCode = AppConstants.ErrorCodes.ValidationFailed;
                message = "Invalid request.";
                break;

            case TimeoutException:
                statusCode = HttpStatusCode.RequestTimeout;
                errorCode = AppConstants.ErrorCodes.UnexpectedError;
                message = "The request timed out. Please try again.";
                break;
        }

        var result = ServiceResult.Fail(message, errorCode, (int)statusCode);

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }
}
