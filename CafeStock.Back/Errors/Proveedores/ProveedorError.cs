using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.Proveedores;

public abstract record ProveedorError(string Message) : DomainError(Message)
{
    public sealed record NotFound(int Id)
        : ProveedorError($"No se ha encontrado ningún proveedor");

    public sealed record Validation(IEnumerable<string> Errors)
        : ProveedorError($"Errores de validacion:{Environment.NewLine}• {string.Join($"{Environment.NewLine}•", Errors)}");

    public sealed record DatabaseError(string Details)
        : ProveedorError($"Error de base de datos: {Details}");
}

public static class ProveedorErrors
{
    public static DomainError NotFound(int id) => new ProveedorError.NotFound(id);
    public static DomainError Validation(IEnumerable<string> errors) => new ProveedorError.Validation(errors);
    public static DomainError DatabaseError(string details) => new ProveedorError.DatabaseError(details);
}
