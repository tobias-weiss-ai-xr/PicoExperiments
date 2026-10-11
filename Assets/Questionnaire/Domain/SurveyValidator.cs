using System.Collections.Generic;

namespace Questionnaire
{
    /// <summary>
    /// Validates survey configuration for common errors at runtime or edit-time.
    /// </summary>
    public static class SurveyValidator
    {
        public sealed class ValidationResult
        {
            public bool IsValid { get; private set; } = true;
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();

            public void AddError(string message)
            {
                Errors.Add(message);
                IsValid = false;
            }
            public void AddWarning(string message) => Warnings.Add(message);
            public override string ToString()
            {
                var parts = new List<string>();
                if (!IsValid) parts.Add("INVALID");
                if (Errors.Count > 0) parts.Add("Errors: " + string.Join("; ", Errors));
                if (Warnings.Count > 0) parts.Add("Warnings: " + string.Join("; ", Warnings));
                return string.Join(" | ", parts);
            }
        }

        public static ValidationResult Validate(Survey survey)
        {
            var result = new ValidationResult();
            if (survey == null)
            {
                result.AddError("Survey is null");
                return result;
            }

            // Mandatory fields
            if (string.IsNullOrEmpty(survey.id))
                result.AddError("Survey.id must not be empty");
            else if (!CsvSafe.IsSafe(survey.id))
                result.AddError("Survey.id must be CSV-safe (no ; \\r \\n)");

            if (survey.sections == null || survey.sections.Count == 0)
                result.AddError("Survey must have at least one section");

            var allItemIds = new HashSet<string>();
            var knownTypes = new HashSet<string>(KnownQuestionTypes.V1);
            int totalItems = 0;

            foreach (var section in survey.sections ?? new List<Section>())
            {
                if (string.IsNullOrEmpty(section.id))
                    result.AddError("Section.id must not be empty");
                else if (!CsvSafe.IsSafe(section.id))
                    result.AddError($"Section '{section.id}' id must be CSV-safe");

                if (section.items == null) continue;

                foreach (var item in section.items)
                {
                    totalItems++;

                    // Check item id
                    if (string.IsNullOrEmpty(item.id))
                        result.AddError($"Item in section '{section.id}' has empty id");
                    else if (!CsvSafe.IsSafe(item.id))
                        result.AddError($"Item '{item.id}' id must be CSV-safe");
                    else if (!allItemIds.Add(item.id))
                        result.AddError($"Duplicate item id: '{item.id}'");

                    // Check type
                    if (string.IsNullOrEmpty(item.type))
                        result.AddError($"Item '{item.id}' has no type");
                    else if (!knownTypes.Contains(item.type))
                        result.AddWarning($"Item '{item.id}' has unknown type: '{item.type}'. Known types: {string.Join(", ", knownTypes)}");

                    // Check prompt
                    if (string.IsNullOrEmpty(item.prompt))
                        result.AddWarning($"Item '{item.id}' has empty prompt");
                }
            }

            if (totalItems == 0)
                result.AddError("Survey must have at least one item");

            return result;
        }

        /// <summary>Validate and throw if invalid. Emits warnings via optional callback.</summary>
        public static void ValidateOrThrow(Survey survey, System.Action<string> onWarning = null)
        {
            var result = Validate(survey);
            if (!result.IsValid)
                throw new System.InvalidOperationException("Survey validation failed: " + result.ToString());
            if (result.Warnings.Count > 0 && onWarning != null)
                foreach (var w in result.Warnings)
                    onWarning("[SurveyValidator] " + w);
        }
    }
}
