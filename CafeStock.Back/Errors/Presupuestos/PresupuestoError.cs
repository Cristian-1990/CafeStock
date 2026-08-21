using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.Presupuestos;

public abstract record PresupuestoError(string Message) : DomainError(Message)
{
    public sealed record NotFound(int Id)
        : PresupuestoError($"No se ha encontrado ningún presupuesto");

    /// <summary>
    /// A diferencia de NotFound (buscar por Id que no existe), este caso es el resultado
    /// normal y esperado de GetByProveedorMesAnioAsync cuando ese proveedor no tiene todavía
    /// presupuesto asignado para ese mes: no hay Id que mostrar, se busca por clave compuesta.
    /// </summary>
    public sealed record NoAsignadoParaMes(int ProveedorId, int Mes, int Anio)
        : PresupuestoError($"Este proveedor no tiene presupuesto asignado para {Mes}/{Anio}");

    public sealed record ProveedorNoEncontrado(int ProveedorId)
        : PresupuestoError($"No se ha encontrado el proveedor");

    public sealed record Duplicado(int ProveedorId, int Mes, int Anio)
        : PresupuestoError($"Ya existe un presupuesto para este proveedor en {Mes}/{Anio}");

    public sealed record Validation(IEnumerable<string> Errors)
        : PresupuestoError($"Errores de validacion:{Environment.NewLine}• {string.Join($"{Environment.NewLine}•", Errors)}");

    public sealed record DatabaseError(string Details)
        : PresupuestoError($"Error de base de datos: {Details}");
}

public static class PresupuestoErrors
{
    public static DomainError NotFound(int id) => new PresupuestoError.NotFound(id);
    public static DomainError NoAsignadoParaMes(int proveedorId, int mes, int anio) => new PresupuestoError.NoAsignadoParaMes(proveedorId, mes, anio);
    public static DomainError ProveedorNoEncontrado(int proveedorId) => new PresupuestoError.ProveedorNoEncontrado(proveedorId);
    public static DomainError Duplicado(int proveedorId, int mes, int anio) => new PresupuestoError.Duplicado(proveedorId, mes, anio);
    public static DomainError Validation(IEnumerable<string> errors) => new PresupuestoError.Validation(errors);
    public static DomainError DatabaseError(string details) => new PresupuestoError.DatabaseError(details);
}
