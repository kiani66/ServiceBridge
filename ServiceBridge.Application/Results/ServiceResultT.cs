namespace ServiceBridge.Application.Results;

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string? message = null)
        => new ServiceResult<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200
        };

    public static ServiceResult<T> Created(T data, string? message = null)
        => new ServiceResult<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 201
        };

    public static new ServiceResult<T> Fail(string message, string errorCode, int statusCode)
        => new ServiceResult<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            StatusCode = statusCode
        };
}
