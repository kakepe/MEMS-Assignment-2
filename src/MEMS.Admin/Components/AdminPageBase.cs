using MEMS.Admin.Services;
using MEMS.Data.Services;
using Microsoft.AspNetCore.Components;

namespace MEMS.Admin.Components;

public abstract class AdminPageBase : ComponentBase
{
    [Inject] protected AdminOperations Operations { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    protected ElectionActor? Actor;
    protected bool Busy;
    protected string? Error;
    protected int ErrorStatus;
    protected string? Success;
    protected bool Super => Actor?.IsSuperAdmin == true;

    protected async Task TryAsync(Func<Task> action)
    {
        if (Busy) return;
        Busy = true;
        Error = null;
        ErrorStatus = 0;
        try { await action(); }
        catch (ApiFault fault)
        {
            ErrorStatus = fault.Status;
            Error = fault.Message;
            if (fault.Errors is not null) Error += " " + string.Join(" ", fault.Errors.Values.SelectMany(x => x).Distinct());
        }
        catch (Exception) { ErrorStatus = 500; Error = "This page could not complete the request. Please reload and try again."; }
        finally { Busy = false; }
    }

    protected string RouteMode => Navigation.ToAbsoluteUri(Navigation.Uri).AbsolutePath.TrimEnd('/').Split('/').Last();
    protected string Version(string value) => $"\"{value}\"";
    public static string? MediaUrl(string? canonical, string? version = null) => canonical is null ? null
        : "/admin/media" + canonical[4..] + (version is null ? "" : "?v=" + Uri.EscapeDataString(version));
}
