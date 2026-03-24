using backend.Application.LLM;
using ClosedXML.Excel;

namespace Application.IntegrationTests;

/// <summary>Integration-style test: real filesystem + ClosedXML, minimal chunk JSON (no API, no pac).</summary>
public class ExcelExportIntegrationTests
{
    private const string WorkflowsJson = """
        [
          {
            "workflow": "new_testflow",
            "file": "test.json",
            "connectors": ["shared_office365"],
            "env_vars_used": ["wmreply_Sample"],
            "trigger": "manual",
            "purpose": "Sample purpose",
            "actions_detected": ["GetItem"]
          }
        ]
        """;

    private const string RelationshipsJson = """
        [
          {
            "from": "screen:ScreenA",
            "to": "workflow:new_testflow",
            "type": "screen_to_workflow",
            "evidence": "test evidence"
          }
        ]
        """;

    [Fact]
    public void Export_WritesXlsx_WithExpectedSheetsAndRows()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "app-int-" + Guid.NewGuid());
        var chunksDir = Path.Combine(baseDir, "chunks");
        var outDir = Path.Combine(baseDir, "out");
        Directory.CreateDirectory(chunksDir);
        Directory.CreateDirectory(outDir);

        try
        {
            File.WriteAllText(Path.Combine(chunksDir, "workflows_detailed.json"), WorkflowsJson);
            File.WriteAllText(Path.Combine(chunksDir, "relationships.json"), RelationshipsJson);

            const string prefix = "IntegrationTest";
            ExcelExport.Export(chunksDir, outDir, prefix);

            var xlsxPath = Path.Combine(outDir, $"{prefix}_Exports.xlsx");
            Assert.True(File.Exists(xlsxPath), $"Expected file: {xlsxPath}");

            using var wb = new XLWorkbook(xlsxPath);
            Assert.Contains("Workflows", wb.Worksheets.Select(w => w.Name));
            Assert.Contains("Screen Workflow Mapping", wb.Worksheets.Select(w => w.Name));

            var wfSheet = wb.Worksheet("Workflows");
            Assert.Equal("new_testflow", wfSheet.Cell(2, 1).GetString());
            Assert.Equal("manual", wfSheet.Cell(2, 2).GetString());

            var mapSheet = wb.Worksheet("Screen Workflow Mapping");
            Assert.Equal("ScreenA", mapSheet.Cell(2, 1).GetString());
            Assert.Equal("new_testflow", mapSheet.Cell(2, 2).GetString());
        }
        finally
        {
            try
            {
                if (Directory.Exists(baseDir))
                    Directory.Delete(baseDir, recursive: true);
            }
            catch
            {
                // ignore cleanup failures on CI
            }
        }
    }
}
