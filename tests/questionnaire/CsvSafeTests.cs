using Questionnaire;
using Xunit;

namespace Tests;

public class CsvSafeTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData(";", ",")]
    [InlineData("a;b", "a,b")]
    [InlineData("\r\n", " ")]
    [InlineData("x\ny", "x y")]
    public void CsvSafe_ReplacesDelimiters(string input, string expected)
    {
        Assert.Equal(expected, CsvSafe.Make(input));
    }
}
