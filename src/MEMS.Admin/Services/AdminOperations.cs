using System.Security.Claims;
using System.Diagnostics;
using MEMS.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Admin.Services;

// Each operation owns a short-lived DI scope, including Identity's DbContext. No scoped
// UserManager/DbContext is retained by an interactive component or shared across operations.
public sealed class AdminOperations(IServiceScopeFactory scopes, AuthenticationStateProvider authentication,
    ILogger<AdminOperations> logger)
{
    private static readonly ActivitySource Traces = new("MEMS.Admin");
    public async Task<T> RunAsync<T>(Func<ElectionApiService, ClaimsPrincipal, Task<T>> action, bool super = false)
    {
        using var activity = Traces.StartActivity("admin.data");
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        await using var scope = scopes.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<ElectionAccess>().RequireAdminAsync(principal, super);
            var result = await action(scope.ServiceProvider.GetRequiredService<ElectionApiService>(), principal);
            logger.LogInformation("Admin data operation completed; trace {TraceId}", Activity.Current?.TraceId.ToString());
            return result;
        }
        catch (ApiFault) { activity?.SetStatus(ActivityStatusCode.Error); throw; }
        catch (DbUpdateConcurrencyException) { throw new ApiFault(409, "This record changed. Reload the page before trying again."); }
        catch (DbUpdateException) { throw new ApiFault(409, "This change conflicts with an existing name, slug or dependent record."); }
        catch (Exception exception)
        {
            logger.LogError("Admin operation failed; error type {ErrorType}", exception.GetType().Name);
            throw new ApiFault(500, "The operation could not be completed. Please try again later.");
        }
    }

    public Task RunAsync(Func<ElectionApiService, ClaimsPrincipal, Task> action, bool super = false) =>
        RunAsync(async (service, principal) => { await action(service, principal); return true; }, super);

    public async Task<ElectionActor> ActorAsync()
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ElectionAccess>()
            .RequireAdminAsync((await authentication.GetAuthenticationStateAsync()).User);
    }
}
