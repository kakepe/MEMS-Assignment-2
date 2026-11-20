using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Admin.Controllers;

[Authorize(Policy = "Administrator")]
public sealed class AdminMediaController(IHttpClientFactory clients) : ControllerBase
{
    [HttpGet("admin/media/{slug}/candidates/{id:int}/photo")]
    public Task<IActionResult> Photo(string slug, int id) => Forward($"/api/{Uri.EscapeDataString(slug)}/candidates/{id}/photo");

    [HttpGet("admin/media/{slug}/municipalities/{id:int}/logo")]
    public Task<IActionResult> Logo(string slug, int id) => Forward($"/api/{Uri.EscapeDataString(slug)}/municipalities/{id}/logo");

    private async Task<IActionResult> Forward(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        // Forward only this browser's Identity cookie/chunks to our own Aspire-discovered API.
        var cookies = Request.Cookies.Where(x => x.Key == ".MEMS.Assignment2.Identity"
            || x.Key.StartsWith(".MEMS.Assignment2.IdentityC", StringComparison.Ordinal) && x.Key[".MEMS.Assignment2.IdentityC".Length..].All(char.IsDigit));
        request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies.Select(x => x.Key + "=" + x.Value)));
        using var response = await clients.CreateClient("mems-api").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted), response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }
}
