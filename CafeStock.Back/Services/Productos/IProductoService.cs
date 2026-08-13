using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;

namespace CafeStock.Back.Services.Productos;

public interface IProductoService
{
    Task<IEnumerable<Producto>> GetAllAsync();
    Task<Result<Producto, DomainError>> GetByIdAsync(int id);
    Task<Result<Producto, DomainError>> CreateAsync(Producto producto);
    Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto);
    Task<Result<Producto, DomainError>> DeleteAsync(int id);
    Task<Result<Producto, DomainError>> ConfirmarRecepcionAsync(int id, int cantidadRecibida);
    Task<IEnumerable<Producto>> GetProductosBajoMinimoAsync();

    /// <summary>
    /// Actualiza únicamente StockActual (modo de recuento restringido). Valida solo que la
    /// cantidad no sea negativa; no pasa por el validador completo de Producto ni puede tocar
    /// ningún otro campo (precio, proveedor, nombre...) por diseño.
    /// </summary>
    Task<Result<Producto, DomainError>> ActualizarStockActualAsync(int id, int nuevaCantidad);
}