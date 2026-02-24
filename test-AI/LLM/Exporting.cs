using System;
using System.Diagnostics;
using System.IO;

namespace backend.Application.LLM;

public static class Exporting
{
    public static int RunProcess(string fileName, string arguments, string workingDir)
    {
        var p = new Process();
        p.StartInfo.FileName = fileName;
        p.StartInfo.Arguments = arguments;
        p.StartInfo.WorkingDirectory = workingDir;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.UseShellExecute = false;

        p.Start();
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout.Trim());
        if (p.ExitCode != 0)
            throw new Exception($"Command failed: {fileName} {arguments}\n{stderr}");

        return p.ExitCode;
    }

    public static void ExportWord(string outDir, string overview, string workflows, string faq)
    {
        RunProcess("pandoc", $"\"{overview}\" -o \"Replybrary_Overview.docx\" --toc", outDir);
        RunProcess("pandoc", $"\"{workflows}\" -o \"Replybrary_Workflows.docx\" --toc", outDir);
        RunProcess("pandoc", $"\"{faq}\" -o \"Replybrary_FAQ.docx\" --toc", outDir);

        // Optional: generated mapping/erd, export them too (if missing this wont fail)
        var map = Path.Combine(outDir, "screen_workflow_mapping.md");
        if (File.Exists(map))
            RunProcess("pandoc", $"\"{map}\" -o \"Replybrary_Screen_Workflow_Mapping.docx\" --toc", outDir);

        var erd = Path.Combine(outDir, "erd.mmd");
        if (File.Exists(erd))
            RunProcess("pandoc", $"\"{erd}\" -o \"Replybrary_ERD_Mermaid.docx\" --toc", outDir);
    }

    public static void ExportPdf(string outDir, string overview, string workflows, string faq)
    {
        var pdfEngine = "--pdf-engine=weasyprint";

        RunProcess("pandoc", $"\"{overview}\" -o \"Replybrary_Overview.pdf\" --toc {pdfEngine}", outDir);
        RunProcess("pandoc", $"\"{workflows}\" -o \"Replybrary_Workflows.pdf\" --toc {pdfEngine}", outDir);
        RunProcess("pandoc", $"\"{faq}\" -o \"Replybrary_FAQ.pdf\" --toc {pdfEngine}", outDir);

        var map = Path.Combine(outDir, "screen_workflow_mapping.md");
        if (File.Exists(map))
            RunProcess("pandoc", $"\"{map}\" -o \"Replybrary_Screen_Workflow_Mapping.pdf\" --toc {pdfEngine}", outDir);

        var erd = Path.Combine(outDir, "erd.mmd");
        if (File.Exists(erd))
            RunProcess("pandoc", $"\"{erd}\" -o \"Replybrary_ERD_Mermaid.pdf\" --toc {pdfEngine}", outDir);
    }

}
