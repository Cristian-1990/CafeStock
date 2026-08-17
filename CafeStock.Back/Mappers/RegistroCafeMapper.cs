using CafeStock.Back.Entity;
using CafeStock.Back.Models;

namespace CafeStock.Back.Mappers;

public static class RegistroCafeMapper
{
    public static RegistroCafeEntity ToEntity(this RegistroCafe registro)
    {
        return new RegistroCafeEntity
        {
            Id = registro.Id,
            ProductoId = registro.ProductoId,
            NombreTrabajador = registro.NombreTrabajador,
            Fecha = registro.Fecha
        };
    }

    public static RegistroCafe ToRegistroCafe(this RegistroCafeEntity entity)
    {
        return new RegistroCafe
        {
            Id = entity.Id,
            ProductoId = entity.ProductoId,
            NombreTrabajador = entity.NombreTrabajador,
            Fecha = entity.Fecha
        };
    }
}
