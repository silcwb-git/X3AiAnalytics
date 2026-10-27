using System.Text.Json;
using X3AiAnalytics.Application.Ai.Contracts;
using X3AiAnalytics.Application.Ai.Prompts;

namespace X3AiAnalytics.Application.Ai.Providers;

public class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiProvider(HttpClient http, string apiKey, string model = "gpt-4o-mini")
    {
        _http = http;
        _apiKey = apiKey;
        _model = model;
    }

    public string Name => $"openai:{_model}";

    public async Task<IntentResult> ClassifyAsync(string question, CancellationToken ct = default)
    {
        var raw = await AskAsync(AiPrompts.IntentSystem, question, ct);
        return ParseIntent(raw);
    }

    public async Task<PlanResult> PlanAsync(string question, string catalogDescription, CancellationToken ct = default)
    {
        var user = AiPrompts.PlanUserTemplate
            .Replace("{CATALOG}", catalogDescription)
            .Replace("{QUESTION}", question);

        var raw = await AskAsync(AiPrompts.PlanSystem, user, ct);
        return ParsePlan(raw);
    }

    public async Task<ComposeResult> ComposeAsync(string question, string jsonData, CancellationToken ct = default)
    {
        var user = AiPrompts.ComposeUserTemplate
            .Replace("{QUESTION}", question)
            .Replace("{JSON}", jsonData);

        var raw = await AskAsync(AiPrompts.ComposeSystem, user, ct);

        return new ComposeResult
        {
            Answer = raw.Trim(),
            DataWasEmpty = string.IsNullOrWhiteSpace(jsonData) || jsonData.Trim() is "[]" or "{}"
        };
    }

    private async Task<string> AskAsync(string system, string user, CancellationToken ct)
    {
        var payload = new
        {
            model = _model,
            temperature = 0,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI retornou {(int)res.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private static IntentResult ParseIntent(string raw)
    {
        try
        {
            var json = ExtractJson(raw);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new IntentResult
            {
                IsInScope = root.TryGetProperty("isInScope", out var s) && s.GetBoolean(),
                Domain = root.TryGetProperty("domain", out var d) ? d.GetString() ?? "Unknown" : "Unknown",
                Reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? string.Empty : string.Empty
            };
        }
        catch
        {
            return new IntentResult { IsInScope = false, Reason = "Não foi possível interpretar a classificação." };
        }
    }

    private static PlanResult ParsePlan(string raw)
    {
        try
        {
            var json = ExtractJson(raw);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var parameters = new Dictionary<string, string?>();
            if (root.TryGetProperty("parameters", out var p) && p.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in p.EnumerateObject())
                {
                    parameters[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                        ? null
                        : prop.Value.ToString();
                }
            }

            return new PlanResult
            {
                IsInScope = root.TryGetProperty("isInScope", out var s) && s.GetBoolean(),
                QueryName = root.TryGetProperty("queryName", out var q) && q.ValueKind != JsonValueKind.Null
                    ? q.GetString()
                    : null,
                Parameters = parameters,
                Reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? string.Empty : string.Empty
            };
        }
        catch
        {
            return new PlanResult { IsInScope = false, Reason = "Não foi possível interpretar o plano." };
        }
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw.Substring(start, end - start + 1) : "{}";
    }
}