using System.Collections.Generic;
using System.Linq;

namespace Questionnaire
{
public static class SurveyExtensions
{
    public static IEnumerable<Item> Items(this Survey survey)
    {
        if (survey?.sections == null) yield break;
        foreach (var sec in survey.sections)
        foreach (var it in sec.items ?? new List<Item>()){
            yield return it;
        }
    }

    // Helper: build a displayable section header for an item
    public static string SectionHeaderFor(this Item item, Survey survey)
    {
        if (survey?.sections == null || string.IsNullOrEmpty(item.id)) return "";
        foreach (var sec in survey.sections)
            if (sec.items != null && sec.items.Any(it => it.id == item.id))
                return sec.header;
        return "";
    }
}
}
