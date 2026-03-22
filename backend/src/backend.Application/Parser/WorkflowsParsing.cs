using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace backend.Application.Parser;

public static class WorkflowsParsing
{
    public static List<WorkflowDetail> ParseWorkflowsDetailed(DirectoryInfo workflowsDir, HashSet<string> envVarNames)
    {
        var result = new List<WorkflowDetail>();

        var topJson = workflowsDir.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
            .Where(f => !FsHelpers.IsIgnored(f.Name))
            .ToList();

        foreach (var f in topJson)
        {
            var wf = new WorkflowDetail
            {
                Workflow = Path.GetFileNameWithoutExtension(f.Name),
                File = f.Name
            };

            var text = FsHelpers.SafeReadAllText(f.FullName);

            wf.Connectors = ExtractConnectorsFromFlowJson(text).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            wf.EnvVarsUsed = ExtractEnvVarsFromText(text, envVarNames).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            wf.Trigger = TryExtractTriggerSummary(text);

            wf.ActionsDetected = DetectActions(text);
            wf.Purpose = InferWorkflowPurpose(wf.Workflow, text, wf.Trigger, wf.Connectors, wf.EnvVarsUsed, wf.ActionsDetected);

            result.Add(wf);
        }

        var wfFolders = FsHelpers.SafeListDir(workflowsDir).OfType<DirectoryInfo>().ToList();
        foreach (var folder in wfFolders)
        {
            var def = folder.EnumerateFiles("*.json", SearchOption.AllDirectories)
                .FirstOrDefault(x => x.Name.Equals("definition.json", StringComparison.OrdinalIgnoreCase))
                ?? folder.EnumerateFiles("*.json", SearchOption.AllDirectories).FirstOrDefault();

            if (def == null) continue;

            var wf = new WorkflowDetail
            {
                Workflow = folder.Name,
                File = FsHelpers.RelPath(workflowsDir, def.FullName)
            };

            var text = FsHelpers.SafeReadAllText(def.FullName);

            wf.Connectors = ExtractConnectorsFromFlowJson(text).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            wf.EnvVarsUsed = ExtractEnvVarsFromText(text, envVarNames).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            wf.Trigger = TryExtractTriggerSummary(text);

            wf.ActionsDetected = DetectActions(text);
            wf.Purpose = InferWorkflowPurpose(wf.Workflow, text, wf.Trigger, wf.Connectors, wf.EnvVarsUsed, wf.ActionsDetected);

            result.Add(wf);
        }

        return result
            .GroupBy(x => x.Workflow, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => x.Workflow, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> ExtractConnectorsFromFlowJson(string jsonText)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(jsonText)) return new List<string>();

        foreach (Match m in Regex.Matches(jsonText, @"shared_[a-z0-9]+", RegexOptions.IgnoreCase))
            found.Add(m.Value);

        foreach (Match m in Regex.Matches(jsonText, @"/providers/Microsoft\.PowerApps/apis/[a-zA-Z0-9\-_]+", RegexOptions.IgnoreCase))
            found.Add(NormalizeConnector(m.Value));

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static List<string> ExtractEnvVarsFromText(string text, HashSet<string> envVarNames)
    {
        var used = new List<string>();
        if (string.IsNullOrWhiteSpace(text) || envVarNames.Count == 0) return used;

        foreach (var ev in envVarNames)
        {
            if (text.IndexOf(ev, StringComparison.OrdinalIgnoreCase) >= 0)
                used.Add(ev);
        }
        return used;
    }

    public static string? TryExtractTriggerSummary(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText)) return null;

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            JsonElement def;
            if (root.TryGetProperty("definition", out def) == false)
            {
                if (root.TryGetProperty("properties", out var props) &&
                    props.TryGetProperty("definition", out var def2))
                    def = def2;
                else
                    return null;
            }

            if (!def.TryGetProperty("triggers", out var triggers) || triggers.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var trig in triggers.EnumerateObject())
            {
                var trigName = trig.Name;
                var trigObj = trig.Value;

                string? type = null;
                if (trigObj.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
                    type = t.GetString();

                string? kind = null;
                if (trigObj.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String)
                    kind = k.GetString();

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(type)) parts.Add(type);
                if (!string.IsNullOrWhiteSpace(kind)) parts.Add(kind);

                return parts.Count > 0
                    ? $"{trigName}: {string.Join(", ", parts)}"
                    : trigName;
            }

            return null;
        }
        catch
        {
            return null;
        }
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

    static List<string> DetectActions(string jsonText)
    {
        var text = (jsonText ?? "").ToLowerInvariant();
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (text.Contains("get items") || text.Contains("getitems")) found.Add("Get items");
        if (text.Contains("list rows") || text.Contains("listrows")) found.Add("List rows");
        if (text.Contains("create item") || text.Contains("createitem")) found.Add("Create item");
        if (text.Contains("update item") || text.Contains("updateitem")) found.Add("Update item");
        if (text.Contains("delete item") || text.Contains("deleteitem")) found.Add("Delete item");

        if (text.Contains("create file") || text.Contains("createfile")) found.Add("Create file");
        if (text.Contains("get file") || text.Contains("getfile")) found.Add("Get file");
        if (text.Contains("convert") && text.Contains("pdf")) found.Add("Convert to PDF");

        if (text.Contains("send an email") || text.Contains("sendemail") || text.Contains("office365outlook"))
            found.Add("Send email");

        if (text.Contains("post message") || text.Contains("postmessage") || text.Contains("teams"))
            found.Add("Post message");

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static string InferWorkflowPurpose(
        string workflowName,
        string jsonText,
        string? trigger,
        List<string> connectors,
        List<string> envVarsUsed,
        List<string>? actionsDetected)
    {
        var name = (workflowName ?? "").ToLowerInvariant();
        var t = (trigger ?? "").ToLowerInvariant();
        var actions = actionsDetected ?? new List<string>();

        bool scheduled = t.Contains("recurrence");
        bool manual = t.Contains("powerapp") || t.Contains("manual") || t.Contains("request");
        bool itemCreated = (t.Contains("item") && t.Contains("created")) || t.Contains("when an item is created");

        bool mentionsReminder = name.Contains("reminder");
        bool mentionsUpload = name.Contains("upload");
        bool mentionsIdentifier = name.Contains("identifier");
        bool mentionsExchangeRate = name.Contains("exchange") || name.Contains("rate");
        bool mentionsPipedrive = name.Contains("pipedrive");
        bool mentionsPeopleList = name.Contains("peoplelist") || name.Contains("people_list");

        if (mentionsUpload && manual)
            return "Triggered from the app to upload/store a file using configured connectors.";

        if (mentionsReminder && scheduled)
            return "Scheduled reminder flow that notifies users (exact channel depends on configured connectors).";

        if (mentionsExchangeRate && scheduled)
            return "Scheduled flow that refreshes or updates exchange-rate data using configured connectors.";

        if (mentionsPipedrive && scheduled)
            return "Scheduled daily check flow related to Pipedrive (business logic depends on flow actions).";

        if (mentionsIdentifier && itemCreated)
            return "Event-driven flow triggered when new records are created; performs follow-up updates/notifications.";

        if (mentionsPeopleList && (scheduled || itemCreated))
            return "Flow that maintains/syncs people data using configured connectors.";

        if (manual)
            return "Manual flow invoked from the app (PowerApps trigger); performs automated steps using configured connectors and environment variables.";

        if (scheduled)
            return "Scheduled flow (recurrence trigger) that runs automatically and performs automated steps using configured connectors and environment variables.";

        if (itemCreated)
            return "Event-driven flow (item created trigger) that runs automatically when new records are created and performs follow-up automation.";

        if (actions.Count > 0)
            return "Purpose inferred from detected actions: " + string.Join(", ", actions) + ".";

        return "Purpose not fully inferable from current extracted metadata; requires deeper parsing of flow actions for a precise business description.";
    }
}