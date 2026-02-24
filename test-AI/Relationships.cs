using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

<<<<<<< HEAD
namespace SolutionParserApp;
=======
namespace SolutionParser;
>>>>>>> feature/AI

public static class Relationships
{
    public static List<RelationshipEdge> BuildRelationships(
        SolutionReport report,
        HashSet<string> envVarNames,
        DirectoryInfo? canvasDir,
        DirectoryInfo? canvasSrcDir
    )
    {
        var edges = new List<RelationshipEdge>();

        foreach (var app in report.CanvasAppsDetailed)
        {
            var appNode = $"app:{app.App}";

            foreach (var s in app.Screens)
            {
                edges.Add(new RelationshipEdge
                {
                    From = appNode,
                    To = $"screen:{app.App}:{s}",
                    Type = "app_to_screen"
                });
            }

            foreach (var c in app.Connectors)
            {
                edges.Add(new RelationshipEdge
                {
                    From = appNode,
                    To = $"connector:{c}",
                    Type = "app_to_connector"
                });
            }
        }

        foreach (var wf in report.WorkflowsDetailed)
        {
            var wfNode = $"workflow:{wf.Workflow}";

            foreach (var c in wf.Connectors)
            {
                edges.Add(new RelationshipEdge
                {
                    From = wfNode,
                    To = $"connector:{c}",
                    Type = "workflow_to_connector"
                });
            }

            foreach (var ev in wf.EnvVarsUsed)
            {
                edges.Add(new RelationshipEdge
                {
                    From = wfNode,
                    To = $"env:{ev}",
                    Type = "workflow_to_env"
                });
            }
        }

        var wfNames = report.WorkflowsDetailed
            .Select(w => w.Workflow)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (wfNames.Count > 0)
        {
            if (canvasSrcDir != null && canvasSrcDir.Exists)
                AddScreenToWorkflowEdges(canvasSrcDir, wfNames, edges);
        }

        return edges
            .GroupBy(e => $"{e.From}|{e.To}|{e.Type}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    static void AddScreenToWorkflowEdges(
        DirectoryInfo canvasAppsSrcDir,
        List<string> workflowNames,
        List<RelationshipEdge> edges
    )
    {
        var runCall = new Regex(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*Run\s*\(",
            RegexOptions.Compiled | RegexOptions.Singleline);

        var runCallQuoted = new Regex(@"'([^']+)'\s*\.\s*Run\s*\(",
            RegexOptions.Compiled | RegexOptions.Singleline);

        static string Norm(string s) =>
            Regex.Replace(s ?? "", @"[^A-Za-z0-9]+", "").ToLowerInvariant();

        int runMatches = 0;
        int edgesAdded = 0;

        var wfByBase = workflowNames
            .Select(w => new { Full = w, Base = w.Split('-')[0] })
            .GroupBy(x => Norm(x.Base))
            .ToDictionary(g => g.Key, g => g.First().Full);

        var topSrc = FsHelpers.FindDirCaseInsensitive(canvasAppsSrcDir, "Src");
        var topOther = FsHelpers.FindDirCaseInsensitive(canvasAppsSrcDir, "Other");
        var topOtherSrc = topOther != null ? FsHelpers.FindDirCaseInsensitive(topOther, "Src") : null;

        bool looksSingleExport =
            (topSrc != null && topSrc.Exists) ||
            (topOtherSrc != null && topOtherSrc.Exists);

        if (looksSingleExport)
        {
            var appName = "CanvasAppsSrc";

            var screenFiles = Directory
                .EnumerateFiles(canvasAppsSrcDir.FullName, "*.*.yaml", SearchOption.AllDirectories)
                .Where(p =>
                    p.EndsWith(".fx.yaml", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".pa.yaml", StringComparison.OrdinalIgnoreCase))
                .Where(p => !FsHelpers.IsIgnored(Path.GetFileName(p)))
                .ToList();

            foreach (var path in screenFiles)
            {
                var screenName = Path.GetFileNameWithoutExtension(path);

                bool isAppFile =
                    screenName.Equals("App.fx", StringComparison.OrdinalIgnoreCase) ||
                    screenName.Equals("App.pa", StringComparison.OrdinalIgnoreCase);

                var screenNode = isAppFile
                    ? $"app_start:{appName}"
                    : $"screen:{appName}:{screenName}";

                var fileText = FsHelpers.SafeReadAllText(path);
                if (string.IsNullOrWhiteSpace(fileText))
                    continue;

                if (fileText.IndexOf(".Run", StringComparison.OrdinalIgnoreCase) >= 0)
                    Console.WriteLine($"[DEBUG] .Run found in: {path}");

                foreach (Match m in runCall.Matches(fileText))
                {
                    runMatches++;
                    var called = m.Groups[1].Value.Trim();
                    Console.WriteLine($"[DEBUG] Run() call found: {called}   in   {Path.GetFileName(path)}");

                    var calledN = Norm(called);
                    var calledNoN = calledN.StartsWith("n") ? calledN.Substring(1) : calledN;

                    string? match = null;
                    if (wfByBase.TryGetValue(calledN, out var m1)) match = m1;
                    else if (wfByBase.TryGetValue(calledNoN, out var m2)) match = m2;

                    if (match == null)
                    {
                        match = workflowNames.FirstOrDefault(w =>
                        {
                            var wN = Norm(w);
                            return wN == calledN
                                || wN.StartsWith(calledN)
                                || wN.Contains(calledN)
                                || calledN.Contains(wN);
                        });
                    }

                    if (match == null)
                    {
                        Console.WriteLine($"[DEBUG] Run() DID NOT MAP to a workflow: {called}");
                        continue;
                    }

                    edgesAdded++;
                    Console.WriteLine($"[DEBUG] EDGE ADDED: {screenNode} -> workflow:{match}");
                    edges.Add(new RelationshipEdge
                    {
                        From = screenNode,
                        To = $"workflow:{match}",
                        Type = "screen_to_workflow",
                        Evidence = $"{Path.GetFileName(path)}: {called}.Run(...)"
                    });
                }

                foreach (Match m in runCallQuoted.Matches(fileText))
                {
                    runMatches++;
                    var called = m.Groups[1].Value.Trim();
                    Console.WriteLine($"[DEBUG] Run() call found (quoted): {called}   in   {Path.GetFileName(path)}");

                    var calledN = Norm(called);
                    var calledNoN = calledN.StartsWith("n") ? calledN.Substring(1) : calledN;

                    string? match = null;
                    if (wfByBase.TryGetValue(calledN, out var mm1)) match = mm1;
                    else if (wfByBase.TryGetValue(calledNoN, out var mm2)) match = mm2;

                    if (match == null)
                    {
                        match = workflowNames.FirstOrDefault(w =>
                        {
                            var wN = Norm(w);
                            return wN == calledN
                                || wN.StartsWith(calledN)
                                || wN.Contains(calledN)
                                || calledN.Contains(wN);
                        });
                    }

                    if (match == null)
                    {
                        Console.WriteLine($"[DEBUG] Quoted Run() DID NOT MAP to a workflow: {called}");
                        continue;
                    }

                    edgesAdded++;
                    Console.WriteLine($"[DEBUG] EDGE ADDED (quoted): {screenNode} -> workflow:{match}");
                    edges.Add(new RelationshipEdge
                    {
                        From = screenNode,
                        To = $"workflow:{match}",
                        Type = "screen_to_workflow",
                        Evidence = $"{Path.GetFileName(path)}: '{called}'.Run(...)"
                    });
                }
            }

            Console.WriteLine($"[DEBUG] Total Run() matches: {runMatches}");
            Console.WriteLine($"[DEBUG] Total screen->workflow edges added: {edgesAdded}");
            return;
        }

        foreach (var appFolder in FsHelpers.SafeListDir(canvasAppsSrcDir).OfType<DirectoryInfo>())
        {
            var screenFiles = Directory
                .EnumerateFiles(appFolder.FullName, "*.*.yaml", SearchOption.AllDirectories)
                .Where(p =>
                    p.EndsWith(".fx.yaml", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".pa.yaml", StringComparison.OrdinalIgnoreCase))
                .Where(p => !FsHelpers.IsIgnored(Path.GetFileName(p)))
                .ToList();

            foreach (var path in screenFiles)
            {
                var screenName = Path.GetFileNameWithoutExtension(path);

                bool isAppFile =
                    screenName.Equals("App.fx", StringComparison.OrdinalIgnoreCase) ||
                    screenName.Equals("App.pa", StringComparison.OrdinalIgnoreCase);

                var screenNode = isAppFile
                    ? $"app_start:{appFolder.Name}"
                    : $"screen:{appFolder.Name}:{screenName}";

                var fileText = FsHelpers.SafeReadAllText(path);
                if (string.IsNullOrWhiteSpace(fileText))
                    continue;

                if (fileText.IndexOf(".Run", StringComparison.OrdinalIgnoreCase) >= 0)
                    Console.WriteLine($"[DEBUG] .Run found in: {path}");

                foreach (Match m in runCall.Matches(fileText))
                {
                    runMatches++;
                    var called = m.Groups[1].Value.Trim();
                    var calledN = Norm(called);
                    var calledNoN = calledN.StartsWith("n") ? calledN.Substring(1) : calledN;

                    string? match = null;
                    if (wfByBase.TryGetValue(calledN, out var m1)) match = m1;
                    else if (wfByBase.TryGetValue(calledNoN, out var m2)) match = m2;

                    if (match == null)
                    {
                        match = workflowNames.FirstOrDefault(w =>
                        {
                            var wN = Norm(w);
                            return wN == calledN
                                || wN.StartsWith(calledN)
                                || wN.Contains(calledN)
                                || calledN.Contains(wN);
                        });
                    }

                    if (match == null)
                    {
                        Console.WriteLine($"[DEBUG] Run() DID NOT MAP to a workflow: {called}");
                        continue;
                    }

                    edgesAdded++;
                    edges.Add(new RelationshipEdge
                    {
                        From = screenNode,
                        To = $"workflow:{match}",
                        Type = "screen_to_workflow",
                        Evidence = $"{Path.GetFileName(path)}: {called}.Run(...)"
                    });
                }

                foreach (Match m in runCallQuoted.Matches(fileText))
                {
                    runMatches++;
                    var called = m.Groups[1].Value.Trim();
                    Console.WriteLine($"[DEBUG] Run() call found (quoted): {called}   in   {Path.GetFileName(path)}");

                    var calledN = Norm(called);
                    var calledNoN = calledN.StartsWith("n") ? calledN.Substring(1) : calledN;

                    string? match = null;
                    if (wfByBase.TryGetValue(calledN, out var mm1)) match = mm1;
                    else if (wfByBase.TryGetValue(calledNoN, out var mm2)) match = mm2;

                    if (match == null)
                    {
                        match = workflowNames.FirstOrDefault(w =>
                        {
                            var wN = Norm(w);
                            return wN == calledN
                                || wN.StartsWith(calledN)
                                || wN.Contains(calledN)
                                || calledN.Contains(wN);
                        });
                    }

                    if (match == null)
                    {
                        Console.WriteLine($"[DEBUG] Quoted Run() DID NOT MAP to a workflow: {called}");
                        continue;
                    }

                    edgesAdded++;
                    Console.WriteLine($"[DEBUG] EDGE ADDED (quoted): {screenNode} -> workflow:{match}");
                    edges.Add(new RelationshipEdge
                    {
                        From = screenNode,
                        To = $"workflow:{match}",
                        Type = "screen_to_workflow",
                        Evidence = $"{Path.GetFileName(path)}: '{called}'.Run(...)"
                    });
                }
            }
        }

        Console.WriteLine($"[DEBUG] Total Run() matches: {runMatches}");
        Console.WriteLine($"[DEBUG] Total screen->workflow edges added: {edgesAdded}");
    }
}
