// Summary: Wraps external export tooling (pandoc, mermaid-cli) to generate Word/PDF documents and diagrams from solution artifacts.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;

namespace backend.Application.LLM;

// Summary: Provides helper methods for running external export commands and sanitizing Mermaid diagrams.
public static class Exporting
{
    // Summary: Starts a process with the given command/arguments, waits for completion, and optionally throws on non-zero exit.
    public static int RunProcess(string fileName, string arguments, string workingDir, bool isPac=false)
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

        if (!string.IsNullOrWhiteSpace(stdout) && !isPac) Console.WriteLine(stdout.Trim());
        if (p.ExitCode != 0 && !isPac)
            throw new Exception($"Command failed: {fileName} {arguments}\n{stderr}");

        return p.ExitCode;
    }

    // Summary: Uses pandoc to export overview, workflows, FAQ, and optional mapping/ERD files to Word documents.
    public static void ExportWord(string outDir, string overview, string workflows, string faq, string fileNamePrefix = "Export")
    {
        RunProcess("pandoc", $"\"{overview}\" -o \"{fileNamePrefix}_Overview.docx\" --toc", outDir);
        RunProcess("pandoc", $"\"{workflows}\" -o \"{fileNamePrefix}_Workflows.docx\" --toc", outDir);
        RunProcess("pandoc", $"\"{faq}\" -o \"{fileNamePrefix}_FAQ.docx\" --toc", outDir);

        // Optional: generated mapping/erd, export them too (if missing this wont fail)
        var map = Path.Combine(outDir, "screen_workflow_mapping.md");
        if (File.Exists(map))
            RunProcess("pandoc", $"\"{map}\" -o \"{fileNamePrefix}_Screen_Workflow_Mapping.docx\" --toc", outDir);

        var erd = Path.Combine(outDir, "erd.mmd");
        if (File.Exists(erd))
            RunProcess("pandoc", $"\"{erd}\" -o \"{fileNamePrefix}_ERD_Mermaid.docx\" --toc", outDir);
    }

    // Summary: Uses pandoc to export overview, workflows, FAQ, and optional mapping/ERD files to PDF documents.
    public static void ExportPdf(string outDir, string overview, string workflows, string faq, string fileNamePrefix = "Export")
    {
        var pdfEngine = "--pdf-engine=weasyprint";

        RunProcess("pandoc", $"\"{overview}\" -o \"{fileNamePrefix}_Overview.pdf\" --toc {pdfEngine}", outDir);
        RunProcess("pandoc", $"\"{workflows}\" -o \"{fileNamePrefix}_Workflows.pdf\" --toc {pdfEngine}", outDir);
        RunProcess("pandoc", $"\"{faq}\" -o \"{fileNamePrefix}_FAQ.pdf\" --toc {pdfEngine}", outDir);

        var map = Path.Combine(outDir, "screen_workflow_mapping.md");
        if (File.Exists(map))
            RunProcess("pandoc", $"\"{map}\" -o \"{fileNamePrefix}_Screen_Workflow_Mapping.pdf\" --toc {pdfEngine}", outDir);

        var erd = Path.Combine(outDir, "erd.mmd");
        if (File.Exists(erd))
            RunProcess("pandoc", $"\"{erd}\" -o \"{fileNamePrefix}_ERD_Mermaid.pdf\" --toc {pdfEngine}", outDir);
    }

    /// Sanitize Mermaid content so mmdc parser does not fail on parentheses in labels.
    // Summary: Cleans Mermaid source so mermaid-cli can safely render it, especially when labels contain parentheses.
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

    // Resolves bundled puppeteer-config.json (Chromium flags for Docker/root: --no-sandbox, etc.).
    // Summary: Locates the bundled puppeteer-config.json near the assembly or in the LLM folder if present.
    private static string? ResolvePuppeteerConfigPath()
    {
        var dir = Path.GetDirectoryName(typeof(Exporting).Assembly.Location);
        if (string.IsNullOrEmpty(dir))
            return null;
        var nextToDll = Path.Combine(dir, "puppeteer-config.json");
        if (File.Exists(nextToDll))
            return nextToDll;
        var inLlm = Path.Combine(dir, "LLM", "puppeteer-config.json");
        return File.Exists(inLlm) ? inLlm : null;
    }

    /// Render a Mermaid .mmd file to PDF using mermaid-cli (mmdc). Returns the full path to the generated .pdf file.
    // Summary: Renders a Mermaid diagram file to PDF via mermaid-cli, applying sanitization and optional puppeteer config.
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

        var puppeteerCfg = ResolvePuppeteerConfigPath();
        var puppeteerArg = puppeteerCfg != null ? $" -p \"{puppeteerCfg}\"" : "";
        RunProcess("mmdc", $"-i \"{mermaidFileName}\" -o \"{pdfFileName}\"{puppeteerArg}", outDir);
        return Path.Combine(outDir, pdfFileName);
    }

}
