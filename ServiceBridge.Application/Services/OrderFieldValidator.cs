using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Application.Services;

public class OrderFieldValidator : IOrderFieldValidator
{
    public Dictionary<string, string[]> Validate(
        IReadOnlyCollection<OrderFieldDefinition> definitions,
        IReadOnlyCollection<CreateOrderFieldRequest> inputs)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var knownFields = definitions.ToDictionary(
            x => x.FieldKey,
            StringComparer.OrdinalIgnoreCase);
        var submittedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in inputs)
        {
            var key = input.FieldKey?.Trim() ?? string.Empty;

            if (key.Length == 0)
            {
                AddError(errors, nameof(input.FieldKey), "Field key is required.");
                continue;
            }

            if (!submittedFields.Add(key))
            {
                AddError(errors, key, "This field was submitted more than once.");
                continue;
            }

            if (!knownFields.ContainsKey(key))
            {
                AddError(errors, key, "This field is not accepted for the selected service.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.Value))
            {
                AddError(errors, key, "Field value is required.");
            }
        }

        foreach (var definition in definitions.Where(x => x.IsRequired))
        {
            var input = inputs.FirstOrDefault(x =>
                string.Equals(
                    x.FieldKey?.Trim(),
                    definition.FieldKey,
                    StringComparison.OrdinalIgnoreCase));

            if (input == null || string.IsNullOrWhiteSpace(input.Value))
            {
                AddError(errors, definition.FieldKey, $"{definition.Label} is required.");
            }
        }

        return errors.ToDictionary(
            x => x.Key,
            x => x.Value.Distinct().ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddError(
        Dictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = new List<string>();
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
