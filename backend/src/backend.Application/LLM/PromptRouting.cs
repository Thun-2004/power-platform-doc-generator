using System.Text;

namespace backend.Application.LLM; 

public static class PromptRouting
{
    // small router so "ask" looks in the right chunk
    public static string BuildRoutedPrompt(string question)
    {
        var q = question.ToLowerInvariant();

        bool isEdges =
            q.Contains("edge") ||
            q.Contains("edges") ||
            q.Contains("mapping") ||
            q.Contains("map") ||
            q.Contains("relationship") ||
            q.Contains("relationships") ||
            q.Contains("screen_to_workflow") ||
            q.Contains("workflow_to_env") ||
            q.Contains("app_to_screen") ||
            q.Contains("app_to_connector") ||
            q.Contains("workflow_to_connector") ||
            q.Contains("connects") ||
            q.Contains("links");

        bool isErd =
            q.Contains("erd") ||
            q.Contains("entity relationship") ||
            q.Contains("er diagram") ||
            q.Contains("schema") ||
            q.Contains("tables") ||
            q.Contains("fields") ||
            q.Contains("columns") ||
            q.Contains("primary key") ||
            q.Contains("foreign key");

        var sb = new StringBuilder();

        sb.AppendLine("Answer using ONLY the uploaded solution chunks.");
        sb.AppendLine("If the information is not present, say: Not found in uploaded files.");
        sb.AppendLine();

        if (isEdges)
        {
            sb.AppendLine("Routing rule:");
            sb.AppendLine("- You MUST look in the uploaded file named relationships.json when the question is about edges, mappings, or relationships.");
            sb.AppendLine();
        }

        if (isErd)
        {
            sb.AppendLine("Routing rule:");
            sb.AppendLine("- You MUST look in the uploaded file named erd_schema.json when the question is about ERD/schema/tables/fields.");
            sb.AppendLine("- Do NOT invent tables, fields, or relationships.");
            sb.AppendLine();
        }

        sb.AppendLine("Question:");
        sb.AppendLine(question);

        return sb.ToString();
    }
}
