using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Services.Usuarios;

public interface IUsuarioService
{
    /// <summary>Crea los roles Trabajador, Encargado y Admin si todavía no existen (idempotente).</summary>
    Task AsegurarRolesAsync();

    Task<bool> HayUsuariosAsync();

    Task<IReadOnlyList<Usuario>> ListarAsync();

    Task<Result<Usuario, DomainError>> CrearAsync(string nombreUsuario, string password, string rol);

    /// <summary>
    /// Crea el primer administrador. Solo funciona mientras no exista ningún usuario: es la
    /// configuración inicial, sin contraseñas escritas en el código ni en ficheros.
    /// </summary>
    Task<Result<Usuario, DomainError>> CrearPrimerAdminAsync(string nombreUsuario, string password);

    Task<Result<Usuario, DomainError>> CambiarRolAsync(string id, string nuevoRol);

    Task<Result<Usuario, DomainError>> RestablecerPasswordAsync(string id, string nuevaPassword);

    /// <summary>Cambia el nombre de usuario (debe ser único). Conserva rol, contraseña y el resto.</summary>
    Task<Result<Usuario, DomainError>> RenombrarAsync(string id, string nuevoNombre);

    /// <param name="idUsuarioActual">Id de quien pide la baja: no puede eliminarse a sí mismo.</param>
    Task<Result<Usuario, DomainError>> EliminarAsync(string id, string idUsuarioActual);
}
