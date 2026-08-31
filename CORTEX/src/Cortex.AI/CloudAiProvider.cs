using System.Net.Http.Json;
using System.Text.Json;
using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Security;

namespace Cortex.AI;

/// <summary>
/// Optional cloud completion provider. Only ever instantiated when the user has
/// explicitly toggled "Enable Cloud AI" in the Security/Privacy Center for the current
/// workspace — see <see cref="AiPermissionScope.CloudProviderEnabled"/>. The API key is
/// never held in memory as plain text longer than one call; it's re-read from the DPAPI
/// -protected <see cref="ICredentialStore"/> for each request.
/// </summary>
public sealed class CloudAiProvider : IAiProvider
{
    public AiProviderKind Kind => AiProviderKind.Cloud;

    private readonly HttpClient _http;
    private readonly ICredentialStore _credentials;
    private readonly string _credentialKey;
    private readonly Uri _endpoint;
    private readonly string _model;

    public CloudAiProvider(HttpClient http, ICredentialStore credentials, string credentialKey, Uri endpoint, string model)
    {
        _http = http;
        _credentials = credentials;
        _credentialKey = credentialKey;
        _endpoint = endpoint;
        _model = model;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var apiKey = RequireApiKey();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_endpoint, "embeddings"));
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = JsonContent.Create(new { model = _model, input = text });

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return payload.GetProperty("data")[0].GetProperty("embedding").EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, IReadOnlyList<string> retrievedContext, CancellationToken ct)
    {
        var apiKey = RequireApiKey();
        var groundedPrompt = retrievedContext.Count == 0
            ? userPrompt
            : $"{userPrompt}\n\n--- Retrieved repository context (cite by file path) ---\n{string.Join("\n---\n", retrievedContext)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_endpoint, "chat/completions"));
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = JsonContent.Create(new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = groundedPrompt }
            }
        });

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return payload.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private string RequireApiKey() =>
        _credentials.TryRead(_credentialKey)
        ?? throw new InvalidOperationException("No Cloud AI API key stored. Add one in Settings → AI Providers.");
}
