using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.RegistrosCafe;

public abstract record RegistroCafeError(string Message) : DomainError(Message)
{
    public sealed record ProductoNoEncontrado(int ProductoId)
        : RegistroCafeError($"No se ha encontrado el producto");

    public sealed record SinStock(int ProductoId)
        : RegistroCafeError($"No queda stock de este producto");

    public sealed record Validation(IEnumerable<string> Errors)
        : RegistroCafeError($"Errores de validacion:{Environment.NewLine}• {string.Join($"{Environment.NewLine}•", Errors)}");

    public sealed record DatabaseError(string Details)
        : RegistroCafeError($"Error de base de datos: {Details}");
}

public static class RegistroCafeErrors
{
    public static DomainError ProductoNoEncontrado(int productoId) => new RegistroCafeError.ProductoNoEncontrado(productoId);
    public static DomainError SinStock(int productoId) => new RegistroCafeError.SinStock(productoId);
    public static DomainError Validation(IEnumerable<string> errors) => new RegistroCafeError.Validation(errors);
    public static DomainError DatabaseError(string details) => new RegistroCafeError.DatabaseError(details);
}
