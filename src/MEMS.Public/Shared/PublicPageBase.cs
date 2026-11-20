using MEMS.Public.Services;
using Microsoft.AspNetCore.Components;

namespace MEMS.Public.Shared;

public abstract class PublicPageBase : ComponentBase, IDisposable
{
    [Inject] protected PublicApi Api { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    protected bool Loading = true;
    protected string? Error;
    protected int Status;
    private CancellationTokenSource? pending;
    private bool disposed;

    protected async Task LoadAsync(Func<CancellationToken, Task> action)
    {
        pending?.Cancel(); pending?.Dispose();
        using var source = new CancellationTokenSource(); pending = source;
        Loading = true; Error = null; Status = 0;
        try { await action(source.Token); }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { }
        catch (PublicApiException fault) { if (!disposed) { Status = fault.Status; Error = fault.SafeMessage; } }
        catch (Exception) { if (!disposed) { Status = 503; Error = "Election information is temporarily unavailable. Please try again."; } }
        finally { if (!disposed && ReferenceEquals(pending, source)) { Loading = false; pending = null; } }
    }
    protected static void RequireTenant(int expected, params int[] actual)
    {
        if (actual.Any(id => id != expected)) throw new PublicApiException(404);
    }
    protected static int? QueryInteger(string? value)
    {
        if (value is null) return null;
        if (int.TryParse(value, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var number)) return number;
        throw new PublicApiException(400);
    }
    public void Dispose() { disposed = true; pending?.Cancel(); GC.SuppressFinalize(this); }
}
