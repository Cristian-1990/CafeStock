using CafeStock.Back.Errors.Common;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Services.StocksRealizados;

/// <summary>
/// Controla qué proveedores tienen el stock hecho y la recepción pendiente. Realizar stock marca al
/// terminar el recuento de un proveedor; Recepcionar solo enseña los marcados y los quita al confirmar.
/// En todos los métodos, un proveedorId null es el grupo "Sin proveedor asignado".
/// </summary>
public interface IStockRealizadoService
{
    /// <summary>Proveedores con la recepción pendiente. El grupo sin proveedor se representa con null.</summary>
    Task<HashSet<int?>> GetProveedoresPendientesAsync();

    Task<Result<bool, DomainError>> MarcarAsync(int? proveedorId);

    Task<Result<bool, DomainError>> QuitarAsync(int? proveedorId);
}
