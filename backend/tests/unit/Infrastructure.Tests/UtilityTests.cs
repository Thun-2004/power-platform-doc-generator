namespace Infrastructure.Tests;

/// <summary>Tests <see cref="Utility"/> (global namespace, Infrastructure Helpers).</summary>
public class UtilityTests
{
    [Theory]
    [InlineData("overview", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("workflows", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("faq", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("diagrams", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("environment-variables", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("ask", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void OutputTypeToMimeTypeConverter_KnownTypes_ReturnsWordMime(string outputType, string expected)
    {
        var actual = Utility.outputTypeToMimeTypeConverter(outputType);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("unknown-type")]
    [InlineData("erd")]
    [InlineData("")]
    public void OutputTypeToMimeTypeConverter_Unknown_ReturnsOctetStream(string outputType)
    {
        var actual = Utility.outputTypeToMimeTypeConverter(outputType);

        Assert.Equal("application/octet-stream", actual);
    }
}
