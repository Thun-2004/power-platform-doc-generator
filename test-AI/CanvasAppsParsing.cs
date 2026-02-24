using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SolutionParserApp;

public static class CanvasAppsParsing
{
    public static Dictionary<string, List<string>> GroupCanvasApps(DirectoryInfo canvasDir)
    {
        var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var knownSuffixes = new[]
        {
            "_BackgroundImageUri",
            "_DocumentUri.msapp",
            "_AdditionalUris0_identity.json"
        };

        foreach (var item in FsHelpers.SafeListDir(canvasDir))
        {
            if (item is not FileInfo) continue;
            if (item.Name.EndsWith(".meta.xml", StringComparison.OrdinalIgnoreCase))
                continue;

            var name = item.Name;
            var baseName = name;

            foreach (var sfx in knownSuffixes)
            {
                if (name.EndsWith(sfx, StringComparison.Ordinal))
                {
                    baseName = name.Substring(0, name.Length - sfx.Length);
                    break;
                }
            }

            if (!groups.TryGetValue(baseName, out var list))
            {
                list = new List<string>();
                groups[baseName] = list;
            }
            list.Add(name);
        }

        return groups
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase
            );
    }

    public static List<CanvasAppDetail> ParseCanvasAppsDetailed(DirectoryInfo canvasDir, DirectoryInfo? canvasSrcDir)
    {
        var result = new List<CanvasAppDetail>();

        var appNamesFromCanvasApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in FsHelpers.SafeListDir(canvasDir))
        {
            if (item is not FileInfo f) continue;
            if (f.Name.EndsWith("_DocumentUri.msapp", StringComparison.OrdinalIgnoreCase))
            {
                var baseName = f.Name.Substring(0, f.Name.Length - "_DocumentUri.msapp".Length);
                appNamesFromCanvasApps.Add(baseName);
            }
        }

        if (canvasSrcDir != null && canvasSrcDir.Exists)
        {
            var topSrc = FsHelpers.FindDirCaseInsensitive(canvasSrcDir, "Src");
            var topOther = FsHelpers.FindDirCaseInsensitive(canvasSrcDir, "Other");
            var topOtherSrc = topOther != null ? FsHelpers.FindDirCaseInsensitive(topOther, "Src") : null;

            bool looksSingleExport =
                (topSrc != null && topSrc.Exists) ||
                (topOtherSrc != null && topOtherSrc.Exists);

            if (looksSingleExport)
            {
                var appName = "CanvasAppsSrc";
                var detail = new CanvasAppDetail { App = appName };

                var screenPaths = Directory
                    .EnumerateFiles(canvasSrcDir.FullName, "*.*.yaml", SearchOption.AllDirectories)
                    .Where(p =>
                        p.EndsWith(".fx.yaml", StringComparison.OrdinalIgnoreCase) ||
                        p.EndsWith(".pa.yaml", StringComparison.OrdinalIgnoreCase))
                    .Where(p => !FsHelpers.IsIgnored(Path.GetFileName(p)))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var p in screenPaths)
                {
                    var screenName = Path.GetFileNameWithoutExtension(p);
                    if (screenName.Equals("App.fx", StringComparison.OrdinalIgnoreCase) ||
                        screenName.Equals("App.pa", StringComparison.OrdinalIgnoreCase))
                        continue;

                    detail.Screens.Add(screenName);
                    detail.FilesSeen.Add(FsHelpers.RelPath(canvasSrcDir, p));
                }

                var connectionsJson = canvasSrcDir.GetFiles("Connections.json", SearchOption.AllDirectories)
                    .FirstOrDefault(f => f.Name.Equals("Connections.json", StringComparison.OrdinalIgnoreCase));

                if (connectionsJson != null)
                {
                    detail.FilesSeen.Add(FsHelpers.RelPath(canvasSrcDir, connectionsJson.FullName));
                    foreach (var c in ExtractConnectorNamesFromConnectionsJson(connectionsJson.FullName))
                        detail.Connectors.Add(c);
                }

                detail.Screens = detail.Screens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                detail.Connectors = detail.Connectors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                detail.FilesSeen = detail.FilesSeen.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                result.Add(detail);
            }
            else
            {
                foreach (var appFolder in FsHelpers.SafeListDir(canvasSrcDir).OfType<DirectoryInfo>())
                {
                    var detail = new CanvasAppDetail { App = appFolder.Name };

                    var screenPaths = Directory
                        .EnumerateFiles(appFolder.FullName, "*.*.yaml", SearchOption.AllDirectories)
                        .Where(p =>
                            p.EndsWith(".fx.yaml", StringComparison.OrdinalIgnoreCase) ||
                            p.EndsWith(".pa.yaml", StringComparison.OrdinalIgnoreCase))
                        .Where(p => !FsHelpers.IsIgnored(Path.GetFileName(p)))
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var p in screenPaths)
                    {
                        var screenName = Path.GetFileNameWithoutExtension(p);
                        if (screenName.Equals("App.fx", StringComparison.OrdinalIgnoreCase) ||
                            screenName.Equals("App.pa", StringComparison.OrdinalIgnoreCase))
                            continue;

                        detail.Screens.Add(screenName);
                        detail.FilesSeen.Add(FsHelpers.RelPath(appFolder, p));
                    }

                    var connectionsJson = appFolder.GetFiles("Connections.json", SearchOption.AllDirectories)
                        .FirstOrDefault(f => f.Name.Equals("Connections.json", StringComparison.OrdinalIgnoreCase));

                    if (connectionsJson != null)
                    {
                        detail.FilesSeen.Add(FsHelpers.RelPath(appFolder, connectionsJson.FullName));
                        foreach (var c in ExtractConnectorNamesFromConnectionsJson(connectionsJson.FullName))
                            detail.Connectors.Add(c);
                    }

                    detail.Screens = detail.Screens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    detail.Connectors = detail.Connectors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    detail.FilesSeen = detail.FilesSeen.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    result.Add(detail);
                }
            }
        }

        if (result.Count == 0)
        {
            foreach (var appName in appNamesFromCanvasApps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                result.Add(new CanvasAppDetail { App = appName });
        }
        else
        {
            var existing = result.Select(r => r.App).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var appName in appNamesFromCanvasApps)
            {
                if (!existing.Contains(appName))
                    result.Add(new CanvasAppDetail { App = appName });
            }
        }

        return result.OrderBy(x => x.App, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static List<string> ExtractConnectorNamesFromConnectionsJson(string path)
    {
        var text = FsHelpers.SafeReadAllText(path);
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var doc = JsonDocument.Parse(text);
            SolutionParserApp.JsonHelpers.WalkJson(doc.RootElement, (_, val) =>
            {
                if (val.ValueKind == JsonValueKind.String)
                {
                    var s = val.GetString() ?? "";
                    if (LooksLikeConnector(s)) found.Add(NormalizeConnector(s));
                }
            });
        }
        catch
        {
            foreach (Match m in Regex.Matches(text, @"(shared_[a-z0-9]+|/providers/Microsoft\.PowerApps/apis/[a-zA-Z0-9\-_]+)", RegexOptions.IgnoreCase))
                found.Add(NormalizeConnector(m.Value));
        }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static bool LooksLikeConnector(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (s.StartsWith("shared_", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("/providers/Microsoft.PowerApps/apis/", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static string NormalizeConnector(string s)
    {
        var t = s.Trim();
        var marker = "/providers/Microsoft.PowerApps/apis/";
        var idx = t.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (idx >= 0)
        {
            t = t.Substring(idx + marker.Length);
            var slash = t.IndexOf('/');
            if (slash >= 0) t = t.Substring(0, slash);
        }
        return t;
    }
}
