using Questionnaire;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests;

public class SchemaValidationTests
{
    [Fact]
    public void EmptySurveyJson_ThrowsOnParse()
    {
        Assert.Throws<ArgumentException>(() => SurveySchema.Parse(""));
    }

    [Fact]
    public void SurveyWithEmptyId_FailsValidation()
    {
        var survey = new Survey { id = "", title = "t", version = 1, sections = new List<Section>() };
        var errors = SurveySchema.Validate(survey);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("survey.id"));
    }

    [Fact]
    public void NoSections_FailsValidation()
    {
        var survey = new Survey { id = "x", title = "t", version = 1, sections = new List<Section>() };
        var errors = SurveySchema.Validate(survey);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("no sections"));
    }

    [Fact]
    public void ItemWithUnknownType_FailsValidation()
    {
        var survey = new Survey
        {
            id = "x",
            sections = new List<Section> {
                new() { id = "s", items = new List<Item> { new() { id = "i", prompt = "p", type = "unknown" } } }
            }
        };
        var errors = SurveySchema.Validate(survey);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("unknown"));
    }

    [Fact]
    public void DuplicateItemId_FailsValidation()
    {
        var survey = new Survey
        {
            id = "x",
            sections = new List<Section> {
                new() { id = "s", items = new List<Item> {
                    new() { id = "dup", prompt = "a", type = "slider" },
                    new() { id = "dup", prompt = "b", type = "slider" }
                }}
            }
        };
        var errors = SurveySchema.Validate(survey);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("duplicate"));
    }

    [Fact]
    public void IdWithSemicolon_FailsValidation()
    {
        var survey = new Survey
        {
            id = "x",
            sections = new List<Section> {
                new() { id = "s", items = new List<Item> { new() { id = "a;b", prompt = "p", type = "slider" } } }
            }
        };
        var errors = SurveySchema.Validate(survey);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains(";"));
    }

    [Fact]
    public void ValidSurvey_PassesValidation()
    {
        var survey = new Survey
        {
            id = "valid",
            title = "SSQ",
            version = 1,
            sections = new List<Section> {
                new() { id = "part1", header = "Nausea",
                    items = new List<Item> {
                        new() { id = "nauseaA", prompt = "How sick?", type = "slider",
                            config = new Dictionary<string, string> {{ "lowText" ,"none" }}}
                    }}
            }
        };
        var errors = SurveySchema.Validate(survey);
        Assert.Empty(errors);
    }
}
