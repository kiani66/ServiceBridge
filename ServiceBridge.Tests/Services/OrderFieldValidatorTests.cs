using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class OrderFieldValidatorTests
{
    private readonly OrderFieldValidator _validator = new();

    [Fact]
    public void Validate_WhenRequiredFieldIsMissing_ReturnsFieldError()
    {
        var errors = _validator.Validate(
            new[]
            {
                new OrderFieldDefinition
                {
                    FieldKey = "email",
                    Label = "Email",
                    IsRequired = true
                }
            },
            Array.Empty<CreateOrderFieldRequest>());

        Assert.Contains("email", errors.Keys);
    }

    [Fact]
    public void Validate_WhenFieldIsUnknownOrDuplicated_ReturnsErrors()
    {
        var errors = _validator.Validate(
            new[]
            {
                new OrderFieldDefinition { FieldKey = "email", Label = "Email" }
            },
            new[]
            {
                new CreateOrderFieldRequest { FieldKey = "email", Value = "a@example.com" },
                new CreateOrderFieldRequest { FieldKey = "EMAIL", Value = "b@example.com" },
                new CreateOrderFieldRequest { FieldKey = "password", Value = "secret" }
            });

        Assert.Contains("email", errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("password", errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WhenFieldsAreValid_ReturnsNoErrors()
    {
        var errors = _validator.Validate(
            new[]
            {
                new OrderFieldDefinition
                {
                    FieldKey = "email",
                    Label = "Email",
                    IsRequired = true
                }
            },
            new[]
            {
                new CreateOrderFieldRequest
                {
                    FieldKey = "email",
                    Value = "a@example.com"
                }
            });

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenFieldKeyIsBlank_ReturnsValidationError()
    {
        var errors = _validator.Validate(
            Array.Empty<OrderFieldDefinition>(),
            new[]
            {
                new CreateOrderFieldRequest
                {
                    FieldKey = " ",
                    Value = "value"
                }
            });

        Assert.Contains(nameof(CreateOrderFieldRequest.FieldKey), errors.Keys);
    }
}
