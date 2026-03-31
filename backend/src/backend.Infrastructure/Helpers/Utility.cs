
public class Utility
{
    // Summary: Converts a string output type to a MIME type for a Word document export.
    public static string outputTypeToMimeTypeConverter(string outputType)
    {
        switch (outputType)
        {
            case "ask":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; 

            case "overview":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "workflows":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "faq":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "diagrams":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "environment-variables":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
              
            default:
                return "application/octet-stream";
        }
    }
}