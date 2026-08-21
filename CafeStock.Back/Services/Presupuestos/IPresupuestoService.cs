using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;

namespace CafeStock.Back.Services.Presupuestos;

public interface IPresupuestoService
{
    /// <summary>
    /// Todos los presupuestos, de todos los proveedores — usado por Informes (Presupuesto vs
    /// Real) para cruzar el gasto real de un mes con el presupuesto de cada proveedor.
    /// </summary>
    Task<IEnumerable<Presupuesto>> GetAllAsync();

    Task<IEnumerable<Presupuesto>> GetByProveedorAsync(int proveedorId);
    Task<Result<Presupuesto, DomainError>> GetByProveedorMesAnioAsync(int proveedorId, int mes, int anio);
    Task<Result<Presupuesto, DomainError>> CreateAsync(Presupuesto presupuesto);

    /// <summary>
    /// Actualiza únicamente el importe asignado de un presupuesto ya existente.
    /// </summary>
    Task<Result<Presupuesto, DomainError>> ActualizarImporteAsync(int id, decimal nuevoImporte);

    /// <summary>
    /// Borra un presupuesto — vía de corrección cuando se eligió mal el Mes/Año (que no se
    /// puede editar): borrar y crear uno nuevo.
    /// </summary>
    Task<Result<Presupuesto, DomainError>> DeleteAsync(int id);
}
