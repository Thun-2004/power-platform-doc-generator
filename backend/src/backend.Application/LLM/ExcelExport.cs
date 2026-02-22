using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;

namespace backend.Application.LLM;
public static class ExcelExport
{
    public static void Export(string chunksDir, string outDir)
    {
        var workflowsPath = Path.Combine(chunksDir, "workflows_detailed.json");
        var relationshipsPath = Path.Combine(chunksDir, "relationships.json");

        if (!File.Exists(workflowsPath))
            throw new Exception($"Missing: {workflowsPath}");
        if (!File.Exists(relationshipsPath))
            throw new Exception($"Missing: {relationshipsPath}");

        var workflows = JsonSerializer.Deserialize<WorkflowDetail[]>(
            File.ReadAllText(workflowsPath)
        ) ?? Array.Empty<WorkflowDetail>();

        var relationships = JsonSerializer.Deserialize<RelationshipEdge[]>(
            File.ReadAllText(relationshipsPath)
        ) ?? Array.Empty<RelationshipEdge>();

        var wb = new XLWorkbook();

        // -------------------------------
        // Sheet 1: Workflows
        // -------------------------------
        var ws = wb.Worksheets.Add("Workflows");
        ws.Cell(1, 1).Value = "Workflow";
        ws.Cell(1, 2).Value = "Trigger";
        ws.Cell(1, 3).Value = "Purpose";
        ws.Cell(1, 4).Value = "Actions Detected";
        ws.Cell(1, 5).Value = "Connectors";
        ws.Cell(1, 6).Value = "Environment Variables";

        var r = 2;
        foreach (var wf in workflows.OrderBy(w => w.Workflow))
        {
            ws.Cell(r, 1).Value = wf.Workflow;
            ws.Cell(r, 2).Value = wf.Trigger ?? "";
            ws.Cell(r, 3).Value = wf.Purpose ?? "";
            ws.Cell(r, 4).Value = wf.ActionsDetected == null ? "" : string.Join(", ", wf.ActionsDetected);
            ws.Cell(r, 5).Value = string.Join(", ", wf.Connectors);
            ws.Cell(r, 6).Value = string.Join(", ", wf.EnvVarsUsed);
            r++;
        }

        ws.Columns().AdjustToContents();

        // -------------------------------
        // Sheet 2: Screen → Workflow
        // -------------------------------
        var ws2 = wb.Worksheets.Add("Screen Workflow Mapping");
        ws2.Cell(1, 1).Value = "Screen";
        ws2.Cell(1, 2).Value = "Workflow";
        ws2.Cell(1, 3).Value = "Evidence";

        var r2 = 2;
        foreach (var e in relationships.Where(e => e.Type == "screen_to_workflow"))
        {
            ws2.Cell(r2, 1).Value = StripPrefix(e.From, "screen:");
            ws2.Cell(r2, 2).Value = StripPrefix(e.To, "workflow:");
            ws2.Cell(r2, 3).Value = e.Evidence ?? "";
            r2++;
        }

        ws2.Columns().AdjustToContents();

        // Save
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, "Replybrary_Exports.xlsx");
        wb.SaveAs(outPath);
    }

    private static string StripPrefix(string value, string prefix)
    {
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return value.Substring(prefix.Length);
        return value;
    }
}