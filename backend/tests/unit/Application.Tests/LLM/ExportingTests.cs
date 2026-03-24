using backend.Application.LLM;

namespace Application.Tests.LLM;

public class ExportingTests
{
    [Fact]
    public void SanitizeMermaidForMmdc_WhenNull_ReturnsNull()
    {
        // Arrange
        string? content = null;

        // Act
        var result = Exporting.SanitizeMermaidForMmdc(content!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeMermaidForMmdc_WhenWhitespace_ReturnsSame()
    {
        var content = "   \n\t  ";

        var result = Exporting.SanitizeMermaidForMmdc(content);

        Assert.Equal(content, result);
    }

    [Theory]
    [InlineData(
        "flowchart LR\n  A[\"Label (with parens)\"] --> B",
        "flowchart LR\n  A[\"Label -with parens-\"] --> B")]
    public void SanitizeMermaidForMmdc_ReplacesParensInsideQuotedBrackets(string input, string expected)
    {
        var result = Exporting.SanitizeMermaidForMmdc(input);

        Assert.Equal(expected, result);
    }
}