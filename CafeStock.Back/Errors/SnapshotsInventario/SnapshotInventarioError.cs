using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.SnapshotsInventario;

public abstract record SnapshotInventarioError(string Message) : DomainError(Message)
{
    public sealed record NotFound(DateOnly Fecha)
        : SnapshotInventarioError($"No hay ningún snapshot para esta fecha");

    public sealed record DatabaseError(string Details)
        : SnapshotInventarioError($"Error de base de datos: {Details}");
}

public static class SnapshotInventarioErrors
{
    public static DomainError NotFound(DateOnly fecha) => new SnapshotInventarioError.NotFound(fecha);
    public static DomainError DatabaseError(string details) => new SnapshotInventarioError.DatabaseError(details);
}
