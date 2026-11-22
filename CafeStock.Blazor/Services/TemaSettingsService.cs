using Microsoft.JSInterop;

namespace CafeStock.Blazor.Services;

/// <summary>
/// Tema visual elegido en ESTE navegador (localStorage), igual que MascotSettingsService: Scoped, una instancia
/// por circuito, para que una tablet que cambie de tema no lo cambie en las demás. Aplica el tema poniendo
/// <c>data-tema</c> en &lt;html&gt; (app.css redefine las variables) y avisa con <see cref="TemaCambiado"/> para que
/// MainLayout cambie también el tema de MudBlazor. Un script en App.razor lo aplica antes de pintar, sin parpadeo.
/// </summary>
public class TemaSettingsService
{
    private const string StorageKey = "cafestock.tema";

    private readonly IJSRuntime _jsRuntime;
    private bool _initialized;

    public string Actual { get; private set; } = TemaCafeStock.IdCafe;

    public bool EsBlancoNegro => Actual == TemaCafeStock.IdBlancoNegro;

    public event Action<string>? TemaCambiado;

    public TemaSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>Lee el tema guardado. SOLO desde OnAfterRenderAsync (no hay JS durante el prerender).</summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var guardado = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        Actual = guardado == TemaCafeStock.IdBlancoNegro ? TemaCafeStock.IdBlancoNegro : TemaCafeStock.IdCafe;
        _initialized = true;
        TemaCambiado?.Invoke(Actual);
    }

    public async Task SetAsync(string id)
    {
        Actual = id == TemaCafeStock.IdBlancoNegro ? TemaCafeStock.IdBlancoNegro : TemaCafeStock.IdCafe;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, Actual);
        await _jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-tema", Actual);
        TemaCambiado?.Invoke(Actual);
    }

    public Task AlternarAsync() =>
        SetAsync(EsBlancoNegro ? TemaCafeStock.IdCafe : TemaCafeStock.IdBlancoNegro);
}
