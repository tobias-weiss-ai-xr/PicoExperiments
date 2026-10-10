using Questionnaire;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests;

internal sealed class FakeWidget : IQuestionWidget
{
    public string Type => "fake";
    public bool IsComplete { get; set; }
    public string LastValue { get; private set; } = string.Empty;
    public void Configure(Item item) { }
    public string ReadValue() => LastValue;
    public void Show(bool visible) { }
    public FakeWidget WithValue(string v) { LastValue = v ?? string.Empty; IsComplete = true; return this; }
}

internal sealed class SpyLogger : ISurveyLogger
{
    public List<SurveyResponse> Responses { get; } = new();
    public int OpenCount { get; private set; }
    public int CloseCount { get; private set; }
    public void Open(string participantId, string scene, string surveyId) => OpenCount++;
    public void Log(in SurveyResponse r) => Responses.Add(r);
    public void Close() => CloseCount++;
}

public class RunnerTests
{
    private static Survey MakeSurvey(string id, params Item[] items) =>
        new() { id = id, sections = new List<Section> { new() { id = "s", items = new List<Item>(items) } } };

    [Fact]
    public void Runner_WalksItems_AdvancesInOrder()
    {
        var survey = MakeSurvey("test", new Item { id = "i1", prompt = "q1", type = "fake" }, new Item { id = "i2", prompt = "q2", type = "fake" });
        var logger = new SpyLogger();
        long now = 0;
        var runner = new SurveyRunner(survey, logger, () => now);

        Assert.False(runner.IsComplete);
        Assert.Equal("i1", runner.CurrentItem.id);

        var w1 = new FakeWidget().WithValue("ans1");
        runner.AttachWidget(w1);
        var ok = runner.Submit();
        Assert.True(ok);

        now += 1000;
        Assert.False(runner.IsComplete);
        Assert.Equal("i2", runner.CurrentItem.id);
        Assert.Single(logger.Responses);
        Assert.Equal("i1", logger.Responses[0].ItemId);

        var w2 = new FakeWidget().WithValue("ans2");
        runner.AttachWidget(w2);
        runner.Submit();
        Assert.True(runner.IsComplete);
        Assert.Equal(2, logger.Responses.Count);
        Assert.Equal("ans2", logger.Responses[1].Value);
    }

    [Fact]
    public void SubmitWithoutCompleteWidget_ReturnsFalse()
    {
        var survey = MakeSurvey("x", new Item { id = "i", type = "fake" });
        var logger = new SpyLogger();
        var runner = new SurveyRunner(survey, logger, () => 0L);
        var w = new FakeWidget(); // NOT complete
        runner.AttachWidget(w);
        Assert.False(runner.CanSubmit);
        Assert.False(runner.Submit());
        Assert.Empty(logger.Responses);
    }

    [Fact]
    public void ExitOnSubmitOnLastItem_IsCompleteAfterSubmit()
    {
        var survey = MakeSurvey("x", new Item { id = "i1", type = "fake", config = new Dictionary<string, string> { { "exitOnSubmit", "true" } } });
        var runner = new SurveyRunner(survey, new SpyLogger(), () => 0L);
        runner.AttachWidget(new FakeWidget().WithValue("v"));
        runner.Submit();
        Assert.True(runner.IsComplete);
    }

    [Fact]
    public void Close_IsIdempotent()
    {
        var logger = new SpyLogger();
        var survey = MakeSurvey("x", new Item { id = "i", type = "fake" });
        var runner = new SurveyRunner(survey, logger, () => 0L);
        runner.Close();
        runner.Close();
        Assert.Equal(1, logger.CloseCount);
    }

    [Fact]
    public void Submit_LogsResponseViaLogger()
    {
        var logger = new SpyLogger();
        var survey = MakeSurvey("surv", new Item { id = "item1", prompt = "Q", type = "fake" });
        survey.version = 42;
        var runner = new SurveyRunner(survey, logger, () => 1000L);
        runner.AttachWidget(new FakeWidget().WithValue("yes"));
        runner.Submit();

        Assert.Single(logger.Responses);
        var r = logger.Responses[0];
        Assert.Equal("surv", r.SurveyId);
        Assert.Equal(42, r.SurveyVersion);
        Assert.Equal("s", r.SectionId);
        Assert.Equal("item1", r.ItemId);
        Assert.Equal("fake", r.Type);
        Assert.Equal("yes", r.Value);
        Assert.Equal(1000L, r.SubmittedAtEpochMs);
        Assert.InRange(r.DurationSec, 0f, float.Epsilon);
    }
}
