using System.ComponentModel.DataAnnotations;
using ServiceBridge.Application.Services;

namespace ServiceBridge.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PasswordPolicyAttribute : ValidationAttribute
{
    public PasswordPolicyAttribute()
        : base(
            $"Password must be between {PasswordPolicy.MinimumLength} " +
            $"and {PasswordPolicy.MaximumLength} characters.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is string password && PasswordPolicy.IsValid(password);
    }
}
