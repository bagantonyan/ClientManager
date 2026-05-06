using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClientManager.Infrastructure.Presentation.Validators
{
    public static class ValidationFormattingExtensions
    {
        public static string FormatErrors(this ValidationResult result) =>
            string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

        public static string FormatErrors(this ModelStateDictionary modelState) =>
            string.Join("; ", modelState
                .Where(kv => kv.Value!.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e => $"{kv.Key}: {e.ErrorMessage}")));
    }
}