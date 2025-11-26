using System.Text;
using PowerAutomateParser.Models;

namespace PowerAutomateParser.Generators
{
    public static class MermaidGenerators
    {
        // Generate a Mermaid flowchart (flowchart TD) representing the actions
        // and their run-order (edges come from ActionNode.RunAfter).
        public static string GenerateFlow(FlowModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("flowchart TD");

            // Declare a node for every action we discovered. The label includes
            // the action name and, when available, its type.
            foreach (var action in model.Actions)
            {
                var id = SafeId(action.Name);
                var label = action.Type != null ? $"{action.Name} ({action.Type})" : action.Name ?? "action";
                sb.AppendLine($"    {id}[\"{label}\"]");
            }

            // Add a Start node for any node that doesn't declare runAfter parents.
            var noParents = model.Actions.Where(a => a.RunAfter == null || a.RunAfter.Count == 0).ToList();
            if (noParents.Any())
            {
                sb.AppendLine("    Start((Start))");
                foreach (var a in noParents)
                {
                    sb.AppendLine($"    Start --> {SafeId(a.Name)}");
                }
            }

            // Add edges based on runAfter relationships
            foreach (var action in model.Actions)
            {
                foreach (var parent in action.RunAfter)
                {
                    var from = SafeId(parent);
                    var to = SafeId(action.Name);
                    sb.AppendLine($"    {from} --> {to}");
                }
            }

            return sb.ToString();
        }

        // Generate a minimal Mermaid ER diagram using simple heuristics looking
        // at action inputs/outputs. This is intentionally basic — it creates an
        // ER diagram only when we can detect candidate entity names.
        public static string GenerateEr(FlowModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("erDiagram");

            // Heuristic: collect keys or values that mention 'entity', 'table' or 'schema'
            var entities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var action in model.Actions)
            {
                void TryAdd(object? candidate)
                {
                    if (candidate == null) return;
                    var s = candidate.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(s))
                        entities.Add(s);
                }

                if (action.Inputs != null)
                {
                    foreach (var kv in action.Inputs)
                    {
                        if (kv.Key.Contains("entity", StringComparison.OrdinalIgnoreCase)
                            || kv.Key.Contains("table", StringComparison.OrdinalIgnoreCase)
                            || kv.Key.Contains("schema", StringComparison.OrdinalIgnoreCase))
                        {
                            TryAdd(kv.Value);
                        }
                    }
                }

                if (action.Outputs != null)
                {
                    foreach (var kv in action.Outputs)
                    {
                        if (kv.Key.Contains("entity", StringComparison.OrdinalIgnoreCase)
                            || kv.Key.Contains("table", StringComparison.OrdinalIgnoreCase))
                        {
                            TryAdd(kv.Value);
                        }
                    }
                }
            }

            // Render each discovered entity as a simple box with an id field
            if (entities.Count == 0)
            {
                sb.AppendLine("    %% No obvious entities discovered — try using a richer export or enabling Dataverse metadata export.");
            }
            else
            {
                foreach (var e in entities)
                {
                    var safe = SafeName(e);
                    sb.AppendLine($"    {safe} {{");
                    sb.AppendLine("        string id");
                    sb.AppendLine("    }");
                }
            }

            return sb.ToString();
        }

        // Helper to make a safe mermaid id from an arbitrary name. Keeps only
        // letters/numbers/underscores. If result is empty, use a hashed fallback.
        private static string SafeId(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "node_null";
            var id = new string(name.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
            if (string.IsNullOrEmpty(id)) id = "node" + Math.Abs(name.GetHashCode());
            return id;
        }

        // Safe entity name for erDiagram
        private static string SafeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "UnknownEntity";
            var id = new string(name.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
            if (string.IsNullOrEmpty(id)) id = "Entity" + Math.Abs(name.GetHashCode());
            return id;
        }
    }
}
