using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Questionnaire
{
public static class SurveySchema
{
    public static Survey Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("survey json cannot be empty or whitespace", nameof(json));

        return JsonConvert.DeserializeObject<Survey>(json)
            ?? throw new InvalidDataException("failed to parse survey json");
    }

    public static IReadOnlyList<string> Validate(Survey survey, System.Collections.ICollection knownTypes = null)
    {
        var errors = new List<string>();
        knownTypes ??= KnownQuestionTypes.V1;

        if (string.IsNullOrWhiteSpace(survey.id))
            errors.Add("survey.id is empty");

        if (survey.sections == null || survey.sections.Count == 0)
            errors.Add("survey has no sections");

        var seenIds = new HashSet<string>();
        foreach (var sec in survey.sections ?? new List<Section>())
        {
            if (string.IsNullOrWhiteSpace(sec.id))
                errors.Add("section has empty id");

            if (sec.items == null) continue;
            foreach (var it in sec.items)
            {
                if (string.IsNullOrWhiteSpace(it.id))
                    errors.Add("item has empty id");
                else if (!seenIds.Add(it.id))
                    errors.Add("duplicate item id: " + it.id);

                if (string.IsNullOrWhiteSpace(it.type))
                    errors.Add("item '" + (it.id ?? "?") + "' has no type");
                else if (!knownTypes.Contains(it.type))
                    errors.Add("item '" + it.id + "' unknown type: " + it.type);

                if (!CsvSafe.IsSafe(it.id))
                    errors.Add("item id '" + it.id + "' contains unsafe CSV characters");
            }
        }
        return errors;
    }
}
}
