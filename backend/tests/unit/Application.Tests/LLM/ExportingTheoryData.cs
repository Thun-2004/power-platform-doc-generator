using System.Collections.Generic;
using Xunit;
using backend.Application.LLM;

namespace Application.Tests.LLM;

public static class ExportingTheoryData
{
    public static IEnumerable<object?[]> SanitizeCases()
    {
        yield return new object?[]
        {
            "flowchart LR\n  A[\"x (y)\"] --> B",
            "flowchart LR\n  A[\"x -y-\"] --> B"
        };
    }
}

public class ExportingSanitizeMemberDataTests
{
    [Theory]
    [MemberData(nameof(ExportingTheoryData.SanitizeCases), MemberType = typeof(ExportingTheoryData))]
    public void SanitizeMermaidForMmdc_ProducesExpectedOutput(string input, string expected)
    {
        var actual = Exporting.SanitizeMermaidForMmdc(input);
        Assert.Equal(expected, actual);
    }
}