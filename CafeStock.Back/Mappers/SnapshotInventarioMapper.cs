using CafeStock.Back.Entity;
using CafeStock.Back.Models;

namespace CafeStock.Back.Mappers;

public static class SnapshotInventarioMapper
{
    public static SnapshotInventarioEntity ToEntity(this SnapshotInventario snapshot)
    {
        return new SnapshotInventarioEntity
        {
            Id = snapshot.Id,
            Fecha = snapshot.Fecha,
            ValorTotal = snapshot.ValorTotal
        };
    }

    public static SnapshotInventario ToSnapshotInventario(this SnapshotInventarioEntity entity)
    {
        return new SnapshotInventario
        {
            Id = entity.Id,
            Fecha = entity.Fecha,
            ValorTotal = entity.ValorTotal
        };
    }
}
