using System;
using System.Collections.Generic;

namespace Questionnaire;

/// <summary>Port: one UI widget that renders an Item and captures a response.</summary>
public interface IQuestionWidget
{
    string Type { get; }
    void Configure(Item item);
    bool IsComplete { get; }
    string ReadValue();
    void Show(bool visible);
}

/// <summary>Port: persists SurveyResponse rows.</summary>
public interface ISurveyLogger
{
    void Open(string participantId, string scene, string surveyId);
    void Log(in SurveyResponse response);
    void Close();
}

/// <summary>Port: loads a Survey from some source (asset, REST, ...).</summary>
public interface ISurveyDataSource
{
    Survey Load();
}
