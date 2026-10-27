using X3AiAnalytics.Application.Ai.Providers;

namespace X3AiAnalytics.Application.Ai;

public static class AiProviderFactory
{
    public static IAiProvider Create()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new DeterministicAiProvider();
        }

        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        return new OpenAiProvider(http, apiKey, model);
    }
}