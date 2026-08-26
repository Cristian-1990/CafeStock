using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.Compras;

public abstract record CompraError(string Message) : DomainError(Message)
{
    public sealed record NotFound(int Id)
        : CompraError($"No se ha encontrado ninguna compra");

    public sealed record LineaNotFound(int LineaCompraId)
        : CompraError($"No se ha encontrado esa línea de compra");

    public sealed record Validation(IEnumerable<string> Errors)
        : CompraError($"Errores de validacion:{Environment.NewLine}• {string.Join($"{Environment.NewLine}•", Errors)}");

    public sealed record DatabaseError(string Details)
        : CompraError($"Error de base de datos: {Details}");
}

public static class CompraErrors
{
    public static DomainError NotFound(int id) => new CompraError.NotFound(id);
    public static DomainError LineaNotFound(int lineaCompraId) => new CompraError.LineaNotFound(lineaCompraId);
    public static DomainError Validation(IEnumerable<string> errors) => new CompraError.Validation(errors);
    public static DomainError DatabaseError(string details) => new CompraError.DatabaseError(details);
}
