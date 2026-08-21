using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;

namespace CafeStock.Back.Services.Presupuestos;

public interface IPresupuestoService
{
    Task<IEnumerable<Presupuesto>> GetByProveedorAsync(int proveedorId);
    Task<Result<Presupuesto, DomainError>> GetByProveedorMesAnioAsync(int proveedorId, int mes, int anio);
    Task<Result<Presupuesto, DomainError>> CreateAsync(Presupuesto presupuesto);

    /// <summary>
    /// Actualiza únicamente el importe asignado de un presupuesto ya existente.
    /// </summary>
    Task<Result<Presupuesto, DomainError>> ActualizarImporteAsync(int id, decimal nuevoImporte);
}
