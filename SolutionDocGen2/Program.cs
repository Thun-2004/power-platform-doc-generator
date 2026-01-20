using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace SolutionDocGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputPath = args.Length > 0 ? args[0] : "solution.xml";

            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"File not found: {inputPath}");
                Environment.Exit(1);
            }

            XDocument doc;
            try
            {
                doc = XDocument.Load(inputPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load XML: {ex.Message}");
                Environment.Exit(1);
                return;
            }

            var sm = doc.Root?.Element("SolutionManifest");
            if (sm == null)
            {
                Console.Error.WriteLine("SolutionManifest element not found.");
                Environment.Exit(1);
            }

            string GetValue(XElement? parent, string name) =>
                parent?.Element(name)?.Value?.Trim() ?? string.Empty;

            var uniqueName = GetValue(sm, "UniqueName");
            var version = GetValue(sm, "Version");
            var managed = GetValue(sm, "Managed") == "1";

            var publisherEl = sm.Element("Publisher");
            var publisher = new PublisherInfo
            {
                UniqueName = GetValue(publisherEl, "UniqueName"),
                Email = GetValue(publisherEl, "EMailAddress"),
                Website = GetValue(publisherEl, "SupportingWebsiteUrl"),
                CustomizationPrefix = GetValue(publisherEl, "CustomizationPrefix"),
                CustomizationOptionValuePrefix = GetValue(publisherEl, "CustomizationOptionValuePrefix"),
                LocalizedNames = publisherEl?
                    .Element("LocalizedNames")?
                    .Elements("LocalizedName")
                    .Select(x => new LocalizedName
                    {
                        LanguageCode = (string?)x.Attribute("languagecode") ?? "",
                        Description = (string?)x.Attribute("description") ?? ""
                    })
                    .ToList() ?? new List<LocalizedName>()
            };

            var localizedSolutionNames = sm
                .Element("LocalizedNames")?
                .Elements("LocalizedName")
                .Select(x => new LocalizedName
                {
                    LanguageCode = (string?)x.Attribute("languagecode") ?? "",
                    Description = (string?)x.Attribute("description") ?? ""
                })
                .ToList() ?? new List<LocalizedName>();

            var components = sm
                .Element("RootComponents")?
                .Elements()
                .Select(x => new RootComponent
                {
                    Type = (string?)x.Attribute("type") ?? "",
                    Id = (string?)x.Attribute("id") ?? "",
                    Behavior = (string?)x.Attribute("behavior") ?? "",
                    SchemaName = (string?)x.Attribute("schemaName") ?? ""
                })
                .ToList() ?? new List<RootComponent>();

            var componentsByType = components
                .GroupBy(c => c.Type)
                .OrderBy(g => g.Key)
                .ToList();

            var mermaid = MermaidBuilder.BuildFlowchart(uniqueName, publisher, componentsByType);

            var md = MarkdownBuilder.Build(
                uniqueName, version, managed,
                localizedSolutionNames, publisher,
                componentsByType, mermaid
            );

            var outputPath = "solution_doc.md";
            File.WriteAllText(outputPath, md);
            Console.WriteLine($"Generated: {outputPath}");
        }
    }

    class LocalizedName
    {
        public string LanguageCode { get; set; } = "";
        public string Description { get; set; } = "";
    }

    class PublisherInfo
    {
        public string UniqueName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Website { get; set; } = "";
        public string CustomizationPrefix { get; set; } = "";
        public string CustomizationOptionValuePrefix { get; set; } = "";
        public List<LocalizedName> LocalizedNames { get; set; } = new();
    }

    class RootComponent
    {
        public string Type { get; set; } = "";
        public string Id { get; set; } = "";
        public string Behavior { get; set; } = "";
        public string SchemaName { get; set; } = "";
    }


    // --- Builders ------------------------------------------------------------

    static class MermaidBuilder
    {
        public static string BuildFlowchart(
            string solutionUniqueName,
            PublisherInfo publisher,
            List<IGrouping<string, RootComponent>> componentsByType)
        {
            var lines = new List<string>();
            lines.Add("flowchart LR");

            var solNodeId = string.IsNullOrWhiteSpace(solutionUniqueName)
                ? "sol"
                : $"sol_{Sanitize(solutionUniqueName)}";

            lines.Add($"  {solNodeId}([Solution: {Escape(solutionUniqueName)}])");

            var pubNodeId = $"pub_{Sanitize(publisher.UniqueName)}";
            lines.Add($"  {pubNodeId}([Publisher: {Escape(publisher.UniqueName)}])");
            lines.Add($"  {solNodeId} --> {pubNodeId}");

            foreach (var group in componentsByType)
            {
                var typeLabel = MapComponentType(group.Key);
                var subgraphId = "grp_" + Sanitize(typeLabel);
                lines.Add($"  subgraph {subgraphId}[\"{Escape(typeLabel)}\"]");
                int i = 0;
                foreach (var comp in group)
                {
                    i++;
                    var nodeId = $"c_{Sanitize(group.Key)}_{i}_{Guid.NewGuid().ToString("N").Substring(0,6)}";

                    var label = !string.IsNullOrWhiteSpace(comp.SchemaName)
                        ? comp.SchemaName
                        : comp.Id;
                    if (string.IsNullOrWhiteSpace(label))
                        label = "(no id)";
                    lines.Add($"    {nodeId}[\"{Escape(label)}\"]");

                    lines.Add($"    {solNodeId} --> {nodeId}");
                }
                lines.Add("  end");
            }

            return string.Join("\n", lines);
        }

        // Map numeric type to friendly name (extend as needed)
        // These numbers come from Dataverse component type enum.
        // NOTE: This is a minimal map for demonstration.
        public static string MapComponentType(string type)
        {
            return type switch
            {
                "29"  => "Type 29 (App/Unknown)",
                "300" => "Type 300 (Environment Variable Definition/Value)",
                _     => $"Type {type}"
            };
        }

        // Simple id sanitization for Mermaid node ids
        static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "empty";

            // Replace any non-alphanumeric or underscore with underscore
            s = Regex.Replace(s, "[^A-Za-z0-9_]", "_");

            // Mermaid node IDs cannot start with a digit
            if (char.IsDigit(s[0]))
                s = "n_" + s;

            // Clean double underscores
            while (s.Contains("__"))
                s = s.Replace("__", "_");

            return s;
        }

        // Escape label content
        static string Escape(string s)
        {
            return s
                .Replace("\"", "\\\"")
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Replace("{", "\\{")
                .Replace("}", "\\}");
        }
    }

    static class MarkdownBuilder
    {
        public static string Build(
            string uniqueName,
            string version,
            bool managed,
            List<LocalizedName> solutionLocalizedNames,
            PublisherInfo publisher,
            List<IGrouping<string, RootComponent>> componentsByType,
            string mermaidDiagram)
        {
            var md = new List<string>();

            md.Add($"# Solution Documentation: {uniqueName}");
            md.Add("");
            md.Add("## Overview");
            md.Add($"- **Version**: {version}");
            md.Add($"- **Managed**: {(managed ? "Yes" : "No")}");
            md.Add("- **Localized Names**:");
            if (solutionLocalizedNames.Any())
            {
                foreach (var ln in solutionLocalizedNames)
                    md.Add($"  - {ln.Description} (languagecode {ln.LanguageCode})");
            }
            else
            {
                md.Add("  - (none)");
            }

            md.Add("");
            md.Add("## Publisher");
            md.Add($"- **Unique Name**: {publisher.UniqueName}");
            md.Add($"- **Email**: {publisher.Email}");
            md.Add($"- **Website**: {publisher.Website}");
            md.Add($"- **Customization Prefix**: {publisher.CustomizationPrefix}");
            md.Add($"- **Customization Option Value Prefix**: {publisher.CustomizationOptionValuePrefix}");
            if (publisher.LocalizedNames.Any())
            {
                md.Add("- **Localized Names**:");
                foreach (var ln in publisher.LocalizedNames)
                    md.Add($"  - {ln.Description} (languagecode {ln.LanguageCode})");
            }

            md.Add("");
            md.Add("## Components");
            foreach (var group in componentsByType)
            {
                var typeLabel = MermaidBuilder.MapComponentType(group.Key);
                md.Add($"### {typeLabel}");
                foreach (var comp in group)
                {
                    var label = !string.IsNullOrWhiteSpace(comp.SchemaName) ? comp.SchemaName : comp.Id;
                    md.Add($"- {label} (behavior={comp.Behavior})");
                }
                md.Add("");
            }

            md.Add("## Diagram");
            md.Add("```mermaid");
            md.Add(mermaidDiagram);
            md.Add("```");

            return string.Join("\n", md);
        }
    }
}