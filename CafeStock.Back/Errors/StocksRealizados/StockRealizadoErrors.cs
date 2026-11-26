using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.StocksRealizados;

public abstract record StockRealizadoError(string Message) : DomainError(Message)
{
    public sealed record DatabaseError(string Details)
        : StockRealizadoError($"Error de base de datos: {Details}");
}

public static class StockRealizadoErrors
{
    public static DomainError DatabaseError(string details) => new StockRealizadoError.DatabaseError(details);
}
