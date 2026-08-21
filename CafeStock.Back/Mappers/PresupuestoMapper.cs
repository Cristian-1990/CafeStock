using CafeStock.Back.Entity;
using CafeStock.Back.Models;

namespace CafeStock.Back.Mappers;

public static class PresupuestoMapper
{
    public static PresupuestoEntity ToEntity(this Presupuesto presupuesto)
    {
        return new PresupuestoEntity
        {
            Id = presupuesto.Id,
            ProveedorId = presupuesto.ProveedorId,
            Mes = presupuesto.Mes,
            Anio = presupuesto.Anio,
            ImporteAsignado = presupuesto.ImporteAsignado
        };
    }

    public static Presupuesto ToPresupuesto(this PresupuestoEntity entity)
    {
        return new Presupuesto
        {
            Id = entity.Id,
            ProveedorId = entity.ProveedorId,
            Mes = entity.Mes,
            Anio = entity.Anio,
            ImporteAsignado = entity.ImporteAsignado
        };
    }
}
