using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RagCliApp;

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

    /// Sanitize Mermaid content so mmdc parser does not fail on parentheses in labels.
    public static string SanitizeMermaidForMmdc(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;

        // 1) Inside ["..."] replace ( and ) with - so the parser does not see them
        content = Regex.Replace(content, @"\[""([^""]*)""\]", m =>
        {
            var label = m.Groups[1].Value.Replace("(", "-").Replace(")", "-");
            return $@"[""{label}""]";
        });

        // 2) Convert round-form nodes ID(Label) to ID["Label"], including nested parens in Label
        var result = new System.Text.StringBuilder();
        int i = 0;
        while (i < content.Length)
        {
            int runStart = i;
            while (i < content.Length && (char.IsLetterOrDigit(content[i]) || content[i] == '_')) i++;
            if (i >= content.Length) { result.Append(content.AsSpan(runStart)); break; }
            string id = content[runStart..i];
            while (i < content.Length && (content[i] == ' ' || content[i] == '\t')) i++;
            if (i >= content.Length || content[i] != '(')
            {
                result.Append(content.AsSpan(runStart, i - runStart));
                if (i < content.Length) { result.Append(content[i]); i++; }
                continue;
            }
            i++; // skip (
            int depth = 1;
            int labelStart = i;
            while (i < content.Length && depth > 0)
            {
                if (content[i] == '(') depth++;
                else if (content[i] == ')') depth--;
                i++;
            }
            if (depth != 0)
            {
                result.Append(content.AsSpan(runStart, i - runStart));
                continue;
            }
            var label = content[labelStart..(i - 1)].Trim().Replace("(", "-").Replace(")", "-");
            result.Append(id).Append("[\"").Append(label).Append("\"]");
        }
        return result.ToString();
    }

    /// Render a Mermaid .mmd file to PDF using mermaid-cli (mmdc). Returns the full path to the generated .pdf file.
    public static string ExportMermaidToPdf(string outDir, string mermaidFileName, string pdfFileName)
    {
        var mmdPath = Path.Combine(outDir, mermaidFileName);
        if (File.Exists(mmdPath))
        {
            var raw = File.ReadAllText(mmdPath);
            var sanitized = SanitizeMermaidForMmdc(raw);
            if (sanitized != raw)
                File.WriteAllText(mmdPath, sanitized);
        }
        RunProcess("mmdc", $"-i \"{mermaidFileName}\" -o \"{pdfFileName}\"", outDir);
        return Path.Combine(outDir, pdfFileName);
    }

}
