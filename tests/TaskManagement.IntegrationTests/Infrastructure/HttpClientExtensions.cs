using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagement.Application.DTOs;

namespace TaskManagement.IntegrationTests.Infrastructure;

public static class HttpClientExtensions
{
    // The API serializes enums as strings; match that on the test client too.
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<string> RegisterAndAuthenticateAsync(this HttpClient client, string userName = "alice")
    {
        // Password chosen to satisfy: 8+ chars, upper, lower, digit, symbol,
        // ≥4 unique characters — matches the production Identity policy.
        var dto = new RegisterDto(userName, $"{userName}@example.com", "Passw0rd!Strong");
        var res = await client.PostAsJsonAsync("/api/auth/register", dto, JsonOptions);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return body.Token;
    }

    public static Task<T?> GetJson<T>(this HttpClient c, string url) =>
        c.GetFromJsonAsync<T>(url, JsonOptions);

    public static async Task<T?> ReadJson<T>(this HttpResponseMessage r) =>
        await r.Content.ReadFromJsonAsync<T>(JsonOptions);

    public static Task<HttpResponseMessage> PostJson<T>(this HttpClient c, string url, T body) =>
        c.PostAsJsonAsync(url, body, JsonOptions);

    public static Task<HttpResponseMessage> PutJson<T>(this HttpClient c, string url, T body) =>
        c.PutAsJsonAsync(url, body, JsonOptions);

    public static Task<HttpResponseMessage> PatchJson<T>(this HttpClient c, string url, T body) =>
        c.PatchAsJsonAsync(url, body, JsonOptions);
}
