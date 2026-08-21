using Microsoft.JSInterop;

namespace CafeStock.Blazor.Services;

/// <summary>
/// Estado de "¿la mascota está activada?", persistido en localStorage del navegador. Registrado
/// como Scoped (una instancia por circuito de Blazor Server): si fuera Singleton, una tablet
/// desactivando la mascota la apagaría para TODAS las tablets conectadas a la vez, porque
/// Blazor Server comparte el proceso .NET entre todos los circuitos. Cada pestaña/tablet
/// necesita su propio estado.
/// </summary>
public class MascotSettingsService
{
    private const string StorageKey = "cafestock.mascot.enabled";

    private readonly IJSRuntime _jsRuntime;
    private bool _initialized;

    public bool Enabled { get; private set; } = true;

    public event Action<bool>? EnabledChanged;

    public MascotSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Lee el valor guardado en localStorage (por defecto true si la clave no existe todavía).
    /// SOLO se puede llamar desde OnAfterRenderAsync de un componente: el prerender de Blazor
    /// Server no tiene circuito JS todavía, así que un IJSRuntime.InvokeAsync en
    /// OnInitializedAsync lanza una excepción (ya nos ha pasado antes en este proyecto).
    /// Idempotente — si ya se inicializó, no vuelve a leer localStorage.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var valorGuardado = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        Enabled = valorGuardado is null || bool.Parse(valorGuardado);
        _initialized = true;
    }

    public async Task SetEnabledAsync(bool value)
    {
        Enabled = value;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, value ? "true" : "false");
        EnabledChanged?.Invoke(value);
    }
}
