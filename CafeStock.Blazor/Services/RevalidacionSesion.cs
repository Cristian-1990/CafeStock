using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CafeStock.Blazor.Services;

/// <summary>
/// Un circuito de Blazor Server (la conexión de una pestaña) dura horas y guarda quién era el
/// usuario cuando se abrió. Sin esto, si un Admin elimina a un usuario o le cambia el rol, esa
/// pestaña seguiría con los permisos antiguos hasta recargar. Cada pocos minutos se comprueba
/// el sello de seguridad del usuario (UserManager.UpdateSecurityStampAsync lo cambia al
/// modificar rol o contraseña) y, si ya no coincide, se cierra la sesión del circuito.
/// </summary>
public sealed class RevalidacionSesion : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IdentityOptions _opciones;

    public RevalidacionSesion(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, IOptions<IdentityOptions> opciones)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones.Value;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        return await SigueVigenteAsync(userManager, authenticationState.User);
    }

    private async Task<bool> SigueVigenteAsync(UserManager<IdentityUser> userManager, ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return false; // el usuario ya no existe

        var sello = principal.FindFirstValue(_opciones.ClaimsIdentity.SecurityStampClaimType);
        return sello == await userManager.GetSecurityStampAsync(user);
    }
}
