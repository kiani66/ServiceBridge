namespace ServiceBridge.Application.Common;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool isSuccess, T? value, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public IReadOnlyList<string> Errors { get; }

    public string? Error => Errors.Count == 0 ? null : Errors[0];

    public static ServiceResult<T> Success(T value)
    {
        return new ServiceResult<T>(true, value, Array.Empty<string>());
    }

    public static ServiceResult<T> Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return new ServiceResult<T>(false, default, new[] { error });
    }

    public static ServiceResult<T> Failure(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorList = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .ToArray();

        if (errorList.Length == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        return new ServiceResult<T>(false, default, errorList);
    }
}
