namespace ServiceBridge.Application.Services;

public static class PasswordPolicy
{
    public const int MinimumLength = 6;
    public const int MaximumLength = 100;

    public static bool IsValid(string? password)
    {
        return !string.IsNullOrWhiteSpace(password) &&
               password.Length >= MinimumLength &&
               password.Length <= MaximumLength;
    }
}
