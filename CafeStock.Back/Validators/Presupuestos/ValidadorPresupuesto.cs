using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Presupuestos;
using CafeStock.Back.Models;
using CafeStock.Back.Validators.Common;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Validators.Presupuestos;

/// <summary>
/// Valida únicamente los campos propios del presupuesto (importe, mes, año). La existencia
/// del proveedor y la comprobación de duplicado ProveedorId+Mes+Anio necesitan acceso a datos
/// y viven en PresupuestosEfRepository, no aquí: IValidador es síncrono y sin dependencias en
/// todo el proyecto (ver ValidadorCompra, ValidadorProducto), así que una comprobación que
/// necesita la base de datos no encaja en esta capa.
/// </summary>
public class ValidadorPresupuesto : IValidador<Presupuesto>
{
    public Result<Presupuesto, DomainError> Validar(Presupuesto presupuesto)
    {
        var errores = new List<string>();

        if (presupuesto.ImporteAsignado <= 0)
            errores.Add("El importe asignado debe ser mayor que 0");
        if (presupuesto.Mes < 1 || presupuesto.Mes > 12)
            errores.Add("El mes debe estar entre 1 y 12");
        if (presupuesto.Anio < 2024)
            errores.Add("El año debe ser 2024 o posterior");

        if (errores.Any())
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.Validation(errores));
        return Result.Success<Presupuesto, DomainError>(presupuesto);
    }
}
