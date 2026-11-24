using CafeStock.Back.Services.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CafeStock.Blazor.Endpoints;

/// <summary>
/// Inicio y cierre de sesión. Se hacen con endpoints normales (POST de un formulario HTML) y no
/// desde un componente interactivo: escribir la cookie de sesión exige una respuesta HTTP todavía
/// no iniciada, y dentro de un circuito de Blazor Server (SignalR) ya no hay ninguna.
/// Los formularios incluyen el token antifalsificación (UseAntiforgery lo valida aquí).
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            [FromForm] string? usuario, [FromForm] string? password, [FromForm] string? returnUrl,
            [FromForm] string? recordar,
            SignInManager<IdentityUser> signInManager, IUsuarioService usuarios, ILogger<Program> logger) =>
        {
            if (!await usuarios.HayUsuariosAsync())
                return Results.LocalRedirect("/primer-acceso");

            // "Recordar contraseña en este equipo": una casilla sin marcar no envía nada, y entonces la cookie
            // de sesión no persiste (termina al cerrar el navegador). La contraseña en sí nunca se guarda.
            var recordarSesion = recordar == "true";

            var resultado = await signInManager.PasswordSignInAsync(
                usuario?.Trim() ?? string.Empty, password ?? string.Empty, isPersistent: recordarSesion, lockoutOnFailure: true);

            if (resultado.Succeeded)
            {
                logger.LogInformation("Inicio de sesión: {Usuario}", usuario);
                return Results.LocalRedirect(DestinoSeguro(returnUrl));
            }

            logger.LogWarning("Inicio de sesión fallido: {Usuario} (bloqueado: {Bloqueado})", usuario, resultado.IsLockedOut);
            // Mensaje genérico a propósito: no revela si el usuario existe o si la contraseña es la que falla.
            var error = resultado.IsLockedOut ? "bloqueado" : "credenciales";
            return Results.LocalRedirect($"/login?error={error}&returnUrl={Uri.EscapeDataString(DestinoSeguro(returnUrl))}");
        }).AllowAnonymous();

        app.MapPost("/auth/logout", async (SignInManager<IdentityUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect("/login");
        });

        app.MapPost("/auth/primer-acceso", async (
            [FromForm] string? usuario, [FromForm] string? password, [FromForm] string? confirmar,
            IUsuarioService usuarios, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, ILogger<Program> logger) =>
        {
            if (await usuarios.HayUsuariosAsync())
                return Results.LocalRedirect("/login");

            if (password != confirmar)
                return Results.LocalRedirect("/primer-acceso?error=" + Uri.EscapeDataString("Las dos contraseñas no coinciden."));

            await usuarios.AsegurarRolesAsync();
            var creado = await usuarios.CrearPrimerAdminAsync(usuario ?? string.Empty, password ?? string.Empty);
            if (creado.IsFailure)
                return Results.LocalRedirect("/primer-acceso?error=" + Uri.EscapeDataString(creado.Error.Message));

            logger.LogInformation("Configuración inicial: creado el administrador {Usuario}", creado.Value.NombreUsuario);
            var user = await userManager.FindByIdAsync(creado.Value.Id);
            await signInManager.SignInAsync(user!, isPersistent: true);
            return Results.LocalRedirect("/");
        }).AllowAnonymous();

        return app;
    }

    /// <summary>Solo se redirige a rutas locales ("/algo"): evita que un enlace de login lleve a una web externa.</summary>
    private static string DestinoSeguro(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//")
        && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/";
}
