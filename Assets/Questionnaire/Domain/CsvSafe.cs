namespace Questionnaire
{
/// <summary>
/// CSV-safe helpers. Keeps id/value strings safe for ';' -delimited files.
/// Same logic as the FeatureMapRaycaster internal CsvSafe (replace newlines with space, ';' with ',').
/// </summary>
public static class CsvSafe
{
    public static string Make(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return s.Replace("\r", "").Replace("\n", " ").Replace(";", ",");
    }

    public static bool IsSafe(string s)
    {
        if (string.IsNullOrEmpty(s))
            return true;
        return s.IndexOfAny(new[] { ';', '\r', '\n' }) < 0;
    }
}
}
