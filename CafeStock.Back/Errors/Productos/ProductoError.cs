using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.Productos;

public abstract record ProductoError(string Message) : DomainError(Message)
{
    public sealed record NotFound(int Id)
        : ProductoError($"No se ha encontrado ningún producto");

    public sealed record Validation(IEnumerable<string> Errors)
        : ProductoError($"Errores de validacion:{Environment.NewLine}• {string.Join($"{Environment.NewLine}•", Errors)}");

    public sealed record DatabaseError(string Details)
        : ProductoError($"Error de base de datos: {Details}");
}

public static class ProductoErrors
{
    public static DomainError NotFound(int id) => new ProductoError.NotFound(id);
    public static DomainError Validation(IEnumerable<string> errors) => new ProductoError.Validation(errors);
    public static DomainError DatabaseError(string details) => new ProductoError.DatabaseError(details);
}