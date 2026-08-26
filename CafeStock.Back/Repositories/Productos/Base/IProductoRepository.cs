using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;
namespace CafeStock.Back.Repositories.Productos.Base;

public interface IProductoRepository
{
    Task<IEnumerable<Producto>> GetAllAsync();
    Task<Result<Producto, DomainError>> GetByIdAsync(int id);
    Task<Result<Producto, DomainError>> CreateAsync(Producto producto);
    Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto);
    Task<Result<Producto, DomainError>> DeleteAsync(int id);
    Task<Result<Producto, DomainError>> ConfirmarRecepcionAsync(int id, int cantidadRecibida);
    Task<IEnumerable<Producto>> ProductosUrgentes();

    /// <summary>
    /// Actualiza ÚNICAMENTE StockActual, sin tocar ningún otro campo. Pensado para el modo
    /// de recuento restringido (RealizarStock.razor): a diferencia de UpdateAsync, no puede
    /// modificar precio, proveedor, nombre, etc. aunque se llame por error con otros datos.
    /// </summary>
    Task<Result<Producto, DomainError>> ActualizarStockActualAsync(int id, int nuevaCantidad);

    /// <summary>
    /// Actualiza ÚNICAMENTE PrecioUnitario, sin tocar ningún otro campo. Pensado para la
    /// sincronización automática de precio de referencia tras una recepción de compra
    /// (CompraService.CreateAsync): a diferencia de UpdateAsync, no puede modificar stock,
    /// proveedor, nombre, etc. aunque se llame por error con otros datos.
    /// </summary>
    Task<Result<Producto, DomainError>> ActualizarPrecioUnitarioAsync(int id, decimal nuevoPrecio);
}