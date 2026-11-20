using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.Usuarios;

public abstract record UsuarioError(string Message) : DomainError(Message)
{
    public sealed record NotFound(string Id)
        : UsuarioError("No se ha encontrado el usuario");

    public sealed record RolInvalido(string Rol)
        : UsuarioError($"El rol \"{Rol}\" no existe");

    /// <summary>No se puede dejar la aplicación sin ningún administrador (nadie podría gestionar usuarios).</summary>
    public sealed record UltimoAdmin()
        : UsuarioError("No se puede quitar o eliminar al único administrador: la aplicación se quedaría sin nadie que gestione los usuarios");

    public sealed record AutoEliminacion()
        : UsuarioError("No puedes eliminar tu propio usuario");

    /// <summary>Solo se permite crear el primer administrador cuando todavía no existe ningún usuario.</summary>
    public sealed record YaHayUsuarios()
        : UsuarioError("La configuración inicial ya se hizo: ya existen usuarios");

    public sealed record Validation(IEnumerable<string> Errors)
        : UsuarioError($"Errores de validacion:{Environment.NewLine}• {string.Join($"{Environment.NewLine}•", Errors)}");
}

public static class UsuarioErrors
{
    public static DomainError NotFound(string id) => new UsuarioError.NotFound(id);
    public static DomainError RolInvalido(string rol) => new UsuarioError.RolInvalido(rol);
    public static DomainError UltimoAdmin() => new UsuarioError.UltimoAdmin();
    public static DomainError AutoEliminacion() => new UsuarioError.AutoEliminacion();
    public static DomainError YaHayUsuarios() => new UsuarioError.YaHayUsuarios();
    public static DomainError Validation(IEnumerable<string> errors) => new UsuarioError.Validation(errors);
}
