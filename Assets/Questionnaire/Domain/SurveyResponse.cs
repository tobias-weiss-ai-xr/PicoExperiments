namespace Questionnaire
{
/// <summary>
/// A committed response to one survey item. Immutable; logged by the runner.
/// </summary>
public readonly struct SurveyResponse
{
    public string SurveyId { get; }
    public int SurveyVersion { get; }
    public string SectionId { get; }
    public string ItemId { get; }
    public string Type { get; }
    public string Value { get; }
    public long SubmittedAtEpochMs { get; }
    public float DurationSec { get; }

    public SurveyResponse(
        string surveyId, int surveyVersion,
        string sectionId, string itemId,
        string type, string value,
        long submittedAtEpochMs, float durationSec)
    {
        SurveyId = surveyId;
        SurveyVersion = surveyVersion;
        SectionId = sectionId;
        ItemId = itemId;
        Type = type;
        Value = value;
        SubmittedAtEpochMs = submittedAtEpochMs;
        DurationSec = durationSec;
    }
}
}
