using System;

namespace Questionnaire
{
/// <summary>Public ports for the Questionnaire domain (stable API).</summary>

/// <typeparam name="T">Widget type for rendering a question.</typeparam>
public interface IQuestionWidget
{
    string Type { get; }
    bool IsComplete { get; }
    void Configure(Item item);
    string ReadValue();
    void Show(bool visible);
}

/// <summary>Hook for writing survey responses.</summary>
public interface ISurveyLogger : IDisposable
{
    void Open(string participantId, string scene, string surveyId);
    void Log(in SurveyResponse response);
    void Close();
}

/// <summary>Hook for loading survey definitions.</summary>
public interface ISurveyDataSource
{
    Survey Load();
}
}
