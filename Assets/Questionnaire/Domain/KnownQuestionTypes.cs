using System.Collections.Generic;

namespace Questionnaire;

public static class KnownQuestionTypes
{
    public static readonly IReadOnlyList<string> V1 = new List<string>
    {
        "slider",
        "likert5",
        "yesNo",
        "singleChoice",
        "openText"
    };
}
