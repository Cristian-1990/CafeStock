using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Services.Compras;

public interface ICompraService
{
    Task<IEnumerable<Compra>> GetAllAsync();
    Task<Result<Compra, DomainError>> CreateAsync(Compra compra);

    /// <summary>
    /// Actualiza únicamente los datos de facturación (método de pago, número de factura del
    /// proveedor, factura adjunta). Valida solo que el método de pago, si se indica, sea uno
    /// de los admitidos; no pasa por el validador completo de Compra.
    /// </summary>
    Task<Result<Compra, DomainError>> ActualizarDatosFacturaAsync(
        int id, string metodoPago, string numeroFacturaProveedor, string facturaAdjuntaUrl);

    /// <summary>
    /// Actualiza únicamente las notas de una Compra ya registrada.
    /// </summary>
    Task<Result<Compra, DomainError>> ActualizarNotasAsync(int id, string notas);

    /// <summary>
    /// Corrige el precio de una LineaCompra ya registrada (factura ya creada). Si la Compra
    /// dueña de esa línea es la más reciente (Fecha desc, Id desc como desempate) entre todas
    /// las que incluyen ese mismo producto, sincroniza también Producto.PrecioUnitario al
    /// nuevo valor; si hay una compra posterior con ese producto, no toca la referencia.
    /// </summary>
    Task<Result<LineaCompra, DomainError>> ActualizarPrecioLineaAsync(int lineaCompraId, decimal nuevoPrecio);
}
