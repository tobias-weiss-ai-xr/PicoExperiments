using Questionnaire;
using System.Collections.Generic;
using Xunit;

namespace Tests;

public class ValidatorTests
{
    [Fact]
    public void Validate_NullSurvey_ReturnsError()
    {
        var r = SurveyValidator.Validate(null);
        Assert.False(r.IsValid);
        Assert.Contains("null", r.ToString());
    }

    [Fact]
    public void Validate_EmptySurvey_ReturnsError()
    {
        var s = new Survey();
        var r = SurveyValidator.Validate(s);
        Assert.False(r.IsValid);
        Assert.Contains("Survey.id", r.ToString());
    }

    [Fact]
    public void Validate_ValidMinimalSurvey_Passes()
    {
        var s = new Survey
        {
            id = "test",
            sections = new List<Section>
            {
                new() { id = "s1", items = new List<Item> { new() { id = "i1", type = "openText", prompt = "Q1" } } }
            }
        };
        var r = SurveyValidator.Validate(s);
        Assert.True(r.IsValid);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Validate_DuplicateItemIds_ReturnsError()
    {
        var s = new Survey
        {
            id = "test",
            sections = new List<Section>
            {
                new() { id = "s1", items = new List<Item> {
                    new() { id = "dup", type = "openText" },
                    new() { id = "dup", type = "openText" }
                } }
            }
        };
        var r = SurveyValidator.Validate(s);
        Assert.False(r.IsValid);
        Assert.Contains("Duplicate", r.ToString());
    }

    [Fact]
    public void Validate_UnknownType_ReturnsWarning()
    {
        var s = new Survey
        {
            id = "test",
            sections = new List<Section>
            {
                new() { id = "s1", items = new List<Item> { new() { id = "i1", type = "unknownType", prompt = "Q" } } }
            }
        };
        var r = SurveyValidator.Validate(s);
        Assert.True(r.IsValid); // warnings only
        Assert.Contains("unknown", r.ToString());
    }

    [Fact]
    public void Validate_UnsafeId_ReturnsError()
    {
        var s = new Survey { id = "bad;id" };
        var r = SurveyValidator.Validate(s);
        Assert.False(r.IsValid);
        Assert.Contains("CSV-safe", r.ToString());
    }

    [Fact]
    public void Validate_NoItems_ReturnsError()
    {
        var s = new Survey
        {
            id = "test",
            sections = new List<Section> { new() { id = "s1", items = new List<Item>() } }
        };
        var r = SurveyValidator.Validate(s);
        Assert.False(r.IsValid);
        Assert.Contains("at least one item", r.ToString());
    }

    [Fact]
    public void ValidateOrThrow_ValidSurvey_Succeeds()
    {
        var s = new Survey
        {
            id = "ok",
            sections = new List<Section>
            {
                new() { id = "s", items = new List<Item> { new() { id = "i", type = "openText", prompt = "Q" } } }
            }
        };
        SurveyValidator.ValidateOrThrow(s); // no exception
    }

    [Fact]
    public void ValidateOrThrow_InvalidSurvey_Throws()
    {
        var s = new Survey();
        Assert.Throws<System.InvalidOperationException>(() => SurveyValidator.ValidateOrThrow(s));
    }
}
