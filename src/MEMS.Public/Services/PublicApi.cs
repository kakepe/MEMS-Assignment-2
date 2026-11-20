using System.Net.Http.Json;
using System.Text.RegularExpressions;
using MEMS.Shared;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace MEMS.Public.Services;

public sealed class PublicApiException(int status) : Exception("Public election request failed.")
{
    public int Status { get; } = status;
    public string SafeMessage => Status switch
    {
        400 => "Check the search, filter or page values and try again.",
        404 => "This municipality or election record could not be found. It may have been removed or its address changed.",
        _ => "Election information is temporarily unavailable. Please try again."
    };
}

// Only HTTP/DTO operations live here. No database, Identity or administrative client dependencies.
public sealed partial class PublicApi(HttpClient http)
{
    public const string GatewayPrefix = "/mems-public/api/mems-api";
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")] private static partial Regex SlugPattern();
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex ColorPattern();

    public static string Slug(string? value)
    {
        var slug = value?.ToLowerInvariant() ?? "";
        if (slug.Length is 0 or > 80 || !SlugPattern().IsMatch(slug)
            || new[] { "api", "admin", "health", "alive", "mems-public" }.Contains(slug)) throw new PublicApiException(404);
        return slug;
    }
    public static string BrandColor(string color) => ColorPattern().IsMatch(color) ? color : "#285e70";

    private async Task<HttpResponseMessage> SendAsync(string path, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GatewayPrefix + path);
        // Same-origin cookies would otherwise include a localhost Admin session. Browsing stays anonymous.
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);
        var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (!response.IsSuccessStatusCode) { var status = (int)response.StatusCode; response.Dispose(); throw new PublicApiException(status); }
        return response;
    }
    private async Task<T> GetAsync<T>(string path, CancellationToken token)
    {
        using var response = await SendAsync(path, token);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token) ?? throw new PublicApiException(500);
    }
    public Task<List<MunicipalityDirectoryItem>> DirectoryAsync(CancellationToken token) => GetAsync<List<MunicipalityDirectoryItem>>("/api/municipalities", token);
    public async Task<MunicipalityDto> MunicipalityAsync(string slug, CancellationToken token)
    {
        slug = Slug(slug);
        var rows = await GetAsync<List<MunicipalityDto>>($"/api/{slug}/municipalities", token);
        return rows.Count == 1 && rows[0].Slug == slug ? rows[0] : throw new PublicApiException(404);
    }
    public Task<List<PositionDto>> PositionsAsync(string slug, CancellationToken token) => GetAsync<List<PositionDto>>($"/api/{Slug(slug)}/positions", token);
    public Task<PositionDto> PositionAsync(string slug, int id, CancellationToken token) => GetAsync<PositionDto>($"/api/{Slug(slug)}/positions/{id}", token);
    public Task<CandidateDto> CandidateAsync(string slug, int id, CancellationToken token) => GetAsync<CandidateDto>($"/api/{Slug(slug)}/candidates/{id}", token);
    public Task<PageResult<CandidateDto>> CandidatesAsync(string slug, CandidateQuery query, CancellationToken token)
    {
        var path = $"/api/{Slug(slug)}/candidates?page={query.Page}&pageSize={query.PageSize}&sort={Uri.EscapeDataString(query.Sort)}";
        if (!string.IsNullOrWhiteSpace(query.Search)) path += "&search=" + Uri.EscapeDataString(query.Search);
        if (query.PositionId.HasValue) path += "&positionId=" + query.PositionId;
        return GetAsync<PageResult<CandidateDto>>(path, token);
    }
    public Task<FoundationStatus> ConnectivityAsync(CancellationToken token) => GetAsync<FoundationStatus>("/api/foundation", token);

    public async Task<string> ImageAsync(string canonicalPath, string? version, CancellationToken token)
    {
        // Canonical media paths must remain on our API transport, never arbitrary remote URLs or files.
        if (!canonicalPath.StartsWith("/api/", StringComparison.Ordinal) || canonicalPath.Contains("..") || canonicalPath.Contains('\\')
            || canonicalPath.Contains('?') || canonicalPath.Contains('#')) throw new PublicApiException(404);
        using var response = await SendAsync(canonicalPath + (version is null ? "" : "?v=" + Uri.EscapeDataString(version)), token);
        var type = response.Content.Headers.ContentType?.MediaType;
        if (type is not ("image/jpeg" or "image/png" or "image/svg+xml") || response.Content.Headers.ContentLength > 8 * 1024 * 1024) throw new PublicApiException(404);
        var bytes = await response.Content.ReadAsByteArrayAsync(token);
        if (bytes.Length > 8 * 1024 * 1024) throw new PublicApiException(404);
        // An img request would send cookies; this data URL displays bytes fetched anonymously instead.
        return $"data:{type};base64,{Convert.ToBase64String(bytes)}";
    }
}
