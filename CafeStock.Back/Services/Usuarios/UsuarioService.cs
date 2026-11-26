using CafeStock.Back.Auth;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Usuarios;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CafeStock.Back.Services.Usuarios;

/// <summary>
/// Gestión de usuarios sobre ASP.NET Core Identity. Es singleton y abre un ámbito (scope) nuevo
/// por operación: así cada operación usa su propio AppDbContext de vida corta, en vez de
/// arrastrar durante todo un circuito de Blazor un contexto con datos de usuarios que pueden
/// quedar obsoletos (mismo criterio que los repositorios).
/// Cada usuario tiene exactamente un rol de <see cref="Roles"/>.
/// </summary>
public class UsuarioService : IUsuarioService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UsuarioService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task AsegurarRolesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var rol in Roles.Todos)
        {
            if (await roleManager.RoleExistsAsync(rol)) continue;
            var resultado = await roleManager.CreateAsync(new IdentityRole(rol));
            if (!resultado.Succeeded)
                throw new InvalidOperationException(
                    $"No se pudo crear el rol {rol}: {string.Join("; ", resultado.Errors.Select(e => e.Description))}");
            Log.Information("Rol creado: {Rol}", rol);
        }
    }

    public async Task<bool> HayUsuariosAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().Users.AnyAsync();
    }

    public async Task<IReadOnlyList<Usuario>> ListarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var usuarios = new List<Usuario>();
        foreach (var user in await userManager.Users.OrderBy(u => u.UserName).ToListAsync())
            usuarios.Add(await ComoUsuarioAsync(userManager, user));
        return usuarios;
    }

    public async Task<Result<Usuario, DomainError>> CrearAsync(string nombreUsuario, string password, string rol)
    {
        if (!Roles.EsValido(rol))
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.RolInvalido(rol));

        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = new IdentityUser { UserName = nombreUsuario?.Trim() };
        var creacion = await userManager.CreateAsync(user, password ?? string.Empty);
        if (!creacion.Succeeded)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(Descripciones(creacion)));

        var asignacion = await userManager.AddToRoleAsync(user, rol);
        if (!asignacion.Succeeded)
        {
            // No dejar un usuario sin rol: no podría entrar en ninguna página.
            await userManager.DeleteAsync(user);
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(Descripciones(asignacion)));
        }

        Log.Information("Usuario creado: {UserName} con rol {Rol}", user.UserName, rol);
        return Result.Success<Usuario, DomainError>(new Usuario(user.Id, user.UserName!, rol));
    }

    public async Task<Result<Usuario, DomainError>> CrearPrimerAdminAsync(string nombreUsuario, string password)
    {
        if (await HayUsuariosAsync())
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.YaHayUsuarios());

        return await CrearAsync(nombreUsuario, password, Roles.Admin);
    }

    public async Task<Result<Usuario, DomainError>> CambiarRolAsync(string id, string nuevoRol)
    {
        if (!Roles.EsValido(nuevoRol))
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.RolInvalido(nuevoRol));

        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.NotFound(id));

        var rolesActuales = await userManager.GetRolesAsync(user);
        if (rolesActuales.Contains(Roles.Admin) && nuevoRol != Roles.Admin && await EsElUnicoAdminAsync(userManager))
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.UltimoAdmin());

        var quitar = await userManager.RemoveFromRolesAsync(user, rolesActuales);
        if (!quitar.Succeeded)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(Descripciones(quitar)));
        var poner = await userManager.AddToRoleAsync(user, nuevoRol);
        if (!poner.Succeeded)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(Descripciones(poner)));

        // Invalida las sesiones abiertas de este usuario: la próxima revalidación le aplicará el rol nuevo.
        await userManager.UpdateSecurityStampAsync(user);

        Log.Information("Rol cambiado: {UserName} pasa a {Rol}", user.UserName, nuevoRol);
        return Result.Success<Usuario, DomainError>(new Usuario(user.Id, user.UserName!, nuevoRol));
    }

    public async Task<Result<Usuario, DomainError>> RestablecerPasswordAsync(string id, string nuevaPassword)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.NotFound(id));

        // Se valida ANTES de tocar nada: quitar la contraseña y fallar al poner la nueva
        // dejaría al usuario sin poder entrar.
        var errores = new List<string>();
        foreach (var validador in userManager.PasswordValidators)
        {
            var validacion = await validador.ValidateAsync(userManager, user, nuevaPassword ?? string.Empty);
            if (!validacion.Succeeded) errores.AddRange(Descripciones(validacion));
        }
        if (errores.Count > 0)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(errores));

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, nuevaPassword!);
        var actualizacion = await userManager.UpdateAsync(user);
        if (!actualizacion.Succeeded)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(Descripciones(actualizacion)));
        await userManager.UpdateSecurityStampAsync(user);

        Log.Information("Contraseña restablecida: {UserName}", user.UserName);
        return Result.Success<Usuario, DomainError>(await ComoUsuarioAsync(userManager, user));
    }

    public async Task<Result<Usuario, DomainError>> RenombrarAsync(string id, string nuevoNombre)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.NotFound(id));

        var anterior = user.UserName;
        var nombre = nuevoNombre?.Trim() ?? string.Empty;

        // SetUserNameAsync valida (no vacío, caracteres permitidos y nombre no repetido) antes de guardar.
        // Además renueva el sello de seguridad: las sesiones abiertas de este usuario se revalidan.
        var cambio = await userManager.SetUserNameAsync(user, nombre);
        if (!cambio.Succeeded)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(Descripciones(cambio)));

        Log.Information("Usuario renombrado: {Anterior} pasa a {UserName}", anterior, user.UserName);
        return Result.Success<Usuario, DomainError>(await ComoUsuarioAsync(userManager, user));
    }

    public async Task<Result<Usuario, DomainError>> EliminarAsync(string id, string idUsuarioActual)
    {
        if (id == idUsuarioActual)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.AutoEliminacion());

        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.NotFound(id));

        var usuario = await ComoUsuarioAsync(userManager, user);
        if (usuario.Rol == Roles.Admin && await EsElUnicoAdminAsync(userManager))
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.UltimoAdmin());

        var borrado = await userManager.DeleteAsync(user);
        if (!borrado.Succeeded)
            return Result.Failure<Usuario, DomainError>(UsuarioErrors.Validation(Descripciones(borrado)));

        Log.Information("Usuario eliminado: {UserName}", user.UserName);
        return Result.Success<Usuario, DomainError>(usuario);
    }

    private static async Task<bool> EsElUnicoAdminAsync(UserManager<IdentityUser> userManager) =>
        (await userManager.GetUsersInRoleAsync(Roles.Admin)).Count <= 1;

    private static async Task<Usuario> ComoUsuarioAsync(UserManager<IdentityUser> userManager, IdentityUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new Usuario(user.Id, user.UserName ?? string.Empty, roles.FirstOrDefault() ?? string.Empty);
    }

    private static IEnumerable<string> Descripciones(IdentityResult resultado) =>
        resultado.Errors.Select(e => e.Description);
}
