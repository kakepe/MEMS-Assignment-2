using Microsoft.JSInterop;

namespace MEMS.Public.Services;

// Scoped in standalone WASM: memory and JS interop remain entirely inside this tab.
public sealed class VotingGuideStore(IJSRuntime js) : IAsyncDisposable
{
    public sealed class State
    {
        public int[] Ids { get; set; } = [];
        public string? Error { get; set; }
        public string? Notice { get; set; }
    }
    private const string Unavailable = "Session storage is unavailable. Allow this site's session storage and reload to use your guide.";
    private readonly Dictionary<int, State> states = [];
    private readonly SemaphoreSlim gate = new(1, 1);
    private Task<IJSObjectReference>? module;
    public event Action? Changed;
    public State? Current(int tenant) => states.GetValueOrDefault(tenant);
    private Task<IJSObjectReference> Module() => module ??= js.InvokeAsync<IJSObjectReference>("import", "./js/voting-guide.js").AsTask();
    private async Task<State> ReadLocked(int tenant)
    {
        if (states.TryGetValue(tenant, out var cached) && cached.Error is null) return cached;
        try { return states[tenant] = await (await Module()).InvokeAsync<State>("read", tenant); }
        catch (JSException) { return states[tenant] = new() { Error = Unavailable }; }
    }
    public async Task<State> ReadAsync(int tenant)
    {
        await gate.WaitAsync();
        try { return await ReadLocked(tenant); }
        finally { gate.Release(); }
    }
    private async Task<State> ChangeAsync(int tenant, Func<State, IEnumerable<int>> change)
    {
        await gate.WaitAsync();
        try
        {
            State result;
            var current = await ReadLocked(tenant);
            if (current.Error is not null) return current;
            try { result = await (await Module()).InvokeAsync<State>("write", tenant, change(current).Distinct().ToArray()); }
            catch (JSException) { result = new() { Error = Unavailable }; }
            states[tenant] = result;
            return result;
        }
        finally { gate.Release(); Changed?.Invoke(); }
    }
    public Task<State> SetAsync(int tenant, IEnumerable<int> ids) => ChangeAsync(tenant, _ => ids);
    public Task<State> ToggleAsync(int tenant, int candidate) => ChangeAsync(tenant,
        current => current.Ids.Contains(candidate) ? current.Ids.Where(id => id != candidate) : current.Ids.Append(candidate));
    public async Task StampAsync() => await (await Module()).InvokeVoidAsync("stampPrintDate");
    public async Task PrintAsync() => await (await Module()).InvokeVoidAsync("printGuide");
    public async ValueTask DisposeAsync() { if (module is { IsCompletedSuccessfully: true }) await (await module).DisposeAsync(); gate.Dispose(); }
}
