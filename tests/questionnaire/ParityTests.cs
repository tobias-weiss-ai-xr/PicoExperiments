using Questionnaire;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests;

public class ParityTests
{
    private static readonly string SsqJsonPath =
        Path.Combine("..", "..", "..", "..", "..", "Assets", "Questionnaires", "ssq.json");

    [Fact]
    public void SsqFixture_ExistsAndIsValidJson()
    {
        var json = File.ReadAllText(SsqJsonPath);
        Assert.NotNull(json);
        Assert.DoesNotContain("TODO", json);
        Assert.DoesNotContain("FIXME", json);
    }

    [Fact]
    public void SsqParse_MatchesLegacyStageCountAndOrder()
    {
        var json = File.ReadAllText(SsqJsonPath);
        var survey = SurveySchema.Parse(json);
        var errors = SurveySchema.Validate(survey);
        Assert.Empty(errors);

        var flat = survey.Items().ToList();
        var legacy = LegacySsq.Stages;

        Assert.Equal(LegacySsq.StageCount, flat.Count);

        for (int i = 0; i < legacy.Count; i++)
        {
            var actual = flat[i];
            var expect = legacy[i];
            Assert.Equal(expect.id, actual.id);
            Assert.Equal(expect.prompt, actual.prompt);
            Assert.Equal("slider", actual.type);
            Assert.Equal(expect.lowText, actual.config["lowText"]);
            Assert.Equal(expect.mediumText, actual.config["mediumText"]);
            Assert.Equal(expect.highText, actual.config["highText"]);
            if (i == legacy.Count - 1)
                Assert.True(actual.ExitAfterSubmit);
            else
                Assert.False(actual.ExitAfterSubmit);
        }
    }
}
