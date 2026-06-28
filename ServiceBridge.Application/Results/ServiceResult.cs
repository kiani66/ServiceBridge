namespace ServiceBridge.Application.Results;

public class ServiceResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public static ServiceResult Ok(string? message = null)
        => new ServiceResult
        {
            Success = true,
            Message = message,
            StatusCode = 200
        };

    public static ServiceResult Created(string? message = null)
        => new ServiceResult
        {
            Success = true,
            Message = message,
            StatusCode = 201
        };

    public static ServiceResult Fail(string message, string errorCode, int statusCode)
        => new ServiceResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            StatusCode = statusCode
        };

    public static ServiceResult ValidationFailed(Dictionary<string, string[]> errors)
        => new ServiceResult
        {
            Success = false,
            Message = "Validation failed.",
            ErrorCode = "ValidationFailed",
            StatusCode = 400,
            ValidationErrors = errors
        };
}
