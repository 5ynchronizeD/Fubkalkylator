using Fubkalkylator.Core;
using Microsoft.JSInterop;

namespace Fubkalkylator.UI;

/// <summary>
/// Sessionslagring i webbläsarens localStorage (via appStore). Fungerar på både webben
/// och Android (MAUI:s WebView behåller localStorage mellan körningar).
/// </summary>
public sealed class JsSessionStore : ISessionStore
{
    private const string Key = "sbSession";
    private readonly IJSRuntime _js;

    public JsSessionStore(IJSRuntime js) => _js = js;

    public async Task<string?> LoadAsync()
    {
        var v = await _js.InvokeAsync<string?>("appStore.get", Key);
        return string.IsNullOrEmpty(v) ? null : v;
    }

    public Task SaveAsync(string value) => _js.InvokeVoidAsync("appStore.set", Key, value).AsTask();
    public Task ClearAsync() => _js.InvokeVoidAsync("appStore.set", Key, "").AsTask();
}
