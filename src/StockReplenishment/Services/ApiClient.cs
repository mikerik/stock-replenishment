using System.Net.Http.Json;
using StockReplenishment.Api;

namespace StockReplenishment.Services;

/// <summary>Used by the Blazor pages. Turns error responses into an exception with the server's message.</summary>
public class ApiClient(HttpClient http)
{
    public Task<List<LocationDto>> GetLocationsAsync() => GetAsync<List<LocationDto>>("api/locations");

    public Task<PagedResult<RequestSummaryDto>> SearchAsync(string query) => GetAsync<PagedResult<RequestSummaryDto>>($"api/requests?{query}");

    public Task<RequestDto> GetRequestAsync(int id) => GetAsync<RequestDto>($"api/requests/{id}");

    public Task<RequestDto> CreateAsync(CreateRequestDto input) => PostAsync("api/requests", input);

    public Task<RequestDto> SubmitAsync(int id) => PostAsync($"api/requests/{id}/submit", null);

    public Task<RequestDto> ApproveAsync(int id) => PostAsync($"api/requests/{id}/approve", null);

    public Task<RequestDto> RejectAsync(int id, string reason) => PostAsync($"api/requests/{id}/reject", new RejectDto(reason));

    public Task<RequestDto> FulfillAsync(int id, FulfillDto input) => PostAsync($"api/requests/{id}/fulfill", input);

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await http.GetAsync(url);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<RequestDto> PostAsync(string url, object? body)
    {
        var response = await http.PostAsJsonAsync(url, body);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<RequestDto>())!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var isJson = response.Content.Headers.ContentType?.MediaType?.EndsWith("json") == true;
        var problem = isJson ? await response.Content.ReadFromJsonAsync<Problem>() : null;
        var message = problem?.Errors?.Values.SelectMany(e => e).FirstOrDefault() ?? problem?.Detail ?? problem?.Title;
        throw new ApiException(message ?? $"Request failed ({(int)response.StatusCode}).");
    }

    private record Problem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}

public class ApiException(string message) : Exception(message);