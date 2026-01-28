

using System;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Files;
using OpenAI.Responses;

namespace backend.Infrastructure;

public class OpenAIGenerate
{
    // public static async Task<string> GenAI(string prompt)
    // {
    //     EnvReader.Load("../../.env");

    //     string prompt = "derive a project description based on this analysis of power platform solution"; 

    //     var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    //     if (string.IsNullOrWhiteSpace(apiKey))
    //         throw new InvalidOperationException("OPENAI_API_KEY is missing.");

    //     var client = new OpenAIClient(apiKey);

    //     var chat = client.GetChatClient("gpt-4.1-nano");

    //     var result = await chat.CompleteChatAsync(
    //         new ChatMessage[]
    //         {
    //             new SystemChatMessage("You are a helpful assistant."),
    //             new UserChatMessage(prompt),
    //         }
    //     );

    //     return result.Value.Content[0].Text;
    // }
  
    public static async Task<string> GenAI(string summaryPath)
    {
        EnvReader.Load("../../.env");

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is missing.");

        if (!File.Exists(summaryPath))
            throw new FileNotFoundException("summary file not found", summaryPath);

        var analysisText = await File.ReadAllTextAsync(summaryPath);

        var prompt =
        $@"Derive a concise project description based on this analysis of a Power Platform solution.

        Rules:
        - Mention: purpose, main components, key data sources, and typical user flow
        - Avoid buzzwords; be specific

        ANALYSIS:
        {analysisText}";

        var client = new OpenAIClient(apiKey);
        var chat = client.GetChatClient("gpt-4.1-nano");

        var result = await chat.CompleteChatAsync(new ChatMessage[]
        {
            new SystemChatMessage("You write clear software project descriptions."),
            new UserChatMessage(prompt),
        });

        return result.Value.Content[0].Text;
    }
}