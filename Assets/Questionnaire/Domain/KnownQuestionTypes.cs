using System.Collections.Generic;

namespace Questionnaire
{
/// <summary>V1 = currently supported question types. ID is lowercase.</summary>
public static class KnownQuestionTypes
{
    public static readonly HashSet<string> V1 = new HashSet<string>
    {
        "slider",
        "likert5",
        "yesno",
        "singlechoice",
        "opentext"
    };
}
}
