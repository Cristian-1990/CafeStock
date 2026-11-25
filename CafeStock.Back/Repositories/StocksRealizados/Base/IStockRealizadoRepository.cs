using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.StocksRealizados.Base;

public interface IStockRealizadoRepository
{
    /// <summary>Todas las marcas vigentes (proveedores con el stock hecho y la recepción pendiente).</summary>
    Task<IEnumerable<StockRealizado>> GetAllAsync();

    /// <summary>
    /// Marca el stock de un proveedor como hecho (ver <see cref="StockRealizado.SinProveedor"/> para el grupo
    /// sin proveedor). Idempotente: si ya había marca, solo se actualiza su fecha.
    /// </summary>
    Task<Result<StockRealizado, DomainError>> MarcarAsync(int proveedorId, DateTime fecha);

    /// <summary>Quita la marca de un proveedor. Idempotente: no hay error si no existía.</summary>
    Task<Result<bool, DomainError>> QuitarAsync(int proveedorId);
}
