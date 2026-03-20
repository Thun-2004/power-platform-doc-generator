using System;
using System.Text.Json;

namespace SolutionParserApp;

public static class JsonHelpers
{
    public static void WalkJson(JsonElement el, Action<string?, JsonElement> onValue, string? key = null)
    {
        onValue(key, el);

        if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in el.EnumerateObject())
                WalkJson(p.Value, onValue, p.Name);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in el.EnumerateArray())
                WalkJson(v, onValue, key);
        }
    }
}
