using System.Collections.Generic;
using System.Linq;

namespace Questionnaire;

public static class SurveyExtensions
{
    public static IEnumerable<Item> Items(this Survey survey)
    {
        if (survey?.sections == null) yield break;
        foreach (var sec in survey.sections)
        foreach (var it in sec.items ?? new List<Item>())
            yield return it;
    }
}
