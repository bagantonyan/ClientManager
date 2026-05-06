using FluentValidation;

namespace ClientManager.Infrastructure.Presentation.Validators
{
    public static class ValidatorExtensions
    {
        public static Dictionary<string, string[]> ValidateCollection<T>(
            this IValidator<T> validator,
            IReadOnlyList<T?> items)
        {
            var errors = new Dictionary<string, string[]>();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (item is null)
                {
                    errors[$"[{i}]"] = new[] { "Item must not be null." };
                    continue;
                }

                var result = validator.Validate(item);
                if (result.IsValid)
                    continue;

                foreach (var err in result.Errors)
                {
                    var key = $"[{i}].{err.PropertyName}";
                    errors[key] = errors.TryGetValue(key, out var existing)
                        ? existing.Append(err.ErrorMessage).ToArray()
                        : new[] { err.ErrorMessage };
                }
            }

            return errors;
        }
    }
}