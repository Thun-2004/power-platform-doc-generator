

using System;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace backend.Infrastructure;

public class OpenAIGenerate
{
    public static async Task<string> GenAI(string prompt)
    {
        EnvReader.Load("../../.env");

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is missing.");

        var client = new OpenAIClient(apiKey);

        var chat = client.GetChatClient("gpt-4.1-nano");

        var result = await chat.CompleteChatAsync(
            new ChatMessage[]
            {
                new SystemChatMessage("You are a helpful assistant."),
                new UserChatMessage(prompt),
            }
        );

        return result.Value.Content[0].Text;
    }
}