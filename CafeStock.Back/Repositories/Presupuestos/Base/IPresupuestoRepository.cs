using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.Presupuestos.Base;

public interface IPresupuestoRepository
{
    Task<IEnumerable<Presupuesto>> GetByProveedorAsync(int proveedorId);

    /// <summary>
    /// Busca el presupuesto de un proveedor para un mes/año concreto, si existe. Se usa tanto
    /// para comprobar duplicados antes de crear como para localizar el registro a editar.
    /// </summary>
    Task<Result<Presupuesto, DomainError>> GetByProveedorMesAnioAsync(int proveedorId, int mes, int anio);

    /// <summary>
    /// Comprueba que el proveedor exista y que no haya ya un presupuesto para el mismo
    /// ProveedorId+Mes+Anio (ver también el índice único en AppDbContext, que es la garantía
    /// real a nivel de base de datos; esta comprobación solo da un error más claro antes de
    /// llegar ahí). No pasa por IValidador porque necesita acceso a datos, y IValidador es
    /// síncrono y sin dependencias, igual que en el resto del proyecto.
    /// </summary>
    Task<Result<Presupuesto, DomainError>> CreateAsync(Presupuesto presupuesto);

    /// <summary>
    /// Actualiza ÚNICAMENTE ImporteAsignado, sin tocar ProveedorId, Mes ni Anio: un presupuesto
    /// pertenece a un proveedor y un mes concretos, así que "editar" es corregir el importe, no
    /// reasignarlo a otro mes (para eso se crearía uno nuevo).
    /// </summary>
    Task<Result<Presupuesto, DomainError>> ActualizarImporteAsync(int id, decimal nuevoImporte);
}
