using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Audit;

public class SensitiveDataRedactorTests
{
    [Fact]
    public void ToRedactedJson_RedactsSensitiveKeysAndKeepsSafeFields()
    {
        var json = SensitiveDataRedactor.ToRedactedJson(new
        {
            Password = "plain-password",
            ApiToken = "token-value",
            ClientSecret = "secret-value",
            CredentialValue = "credential-value",
            Name = "Ali"
        });

        Assert.NotNull(json);
        Assert.Contains("\"Password\":\"[REDACTED]\"", json);
        Assert.Contains("\"ApiToken\":\"[REDACTED]\"", json);
        Assert.Contains("\"ClientSecret\":\"[REDACTED]\"", json);
        Assert.Contains("\"CredentialValue\":\"[REDACTED]\"", json);
        Assert.Contains("\"Name\":\"Ali\"", json);
        Assert.DoesNotContain("plain-password", json);
        Assert.DoesNotContain("token-value", json);
        Assert.DoesNotContain("secret-value", json);
        Assert.DoesNotContain("credential-value", json);
    }

    [Fact]
    public void ToRedactedJson_RedactsNestedSensitiveKeysCaseInsensitive()
    {
        var json = SensitiveDataRedactor.ToRedactedJson(new
        {
            Gateway = new
            {
                AuthorityCode = "auth-code",
                RecoveryKey = "recovery-key",
                PasswordHash = "hash-value",
                OtpCode = "otp-code"
            },
            Safe = new
            {
                DisplayName = "Support"
            }
        });

        Assert.NotNull(json);
        Assert.DoesNotContain("auth-code", json);
        Assert.DoesNotContain("recovery-key", json);
        Assert.DoesNotContain("hash-value", json);
        Assert.DoesNotContain("otp-code", json);
        Assert.Contains("\"DisplayName\":\"Support\"", json);
    }

    [Fact]
    public void RedactDescription_WhenSensitiveKeywordAppears_RedactsWholeDescription()
    {
        var description = SensitiveDataRedactor.RedactDescription(
            "Password reset value was changed.");

        Assert.Equal("[REDACTED]", description);
    }
}
