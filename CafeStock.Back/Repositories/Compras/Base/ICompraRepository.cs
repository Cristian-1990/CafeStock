using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.Compras.Base;

public interface ICompraRepository
{
    Task<IEnumerable<Compra>> GetAllAsync();
    Task<Result<Compra, DomainError>> CreateAsync(Compra compra);

    /// <summary>
    /// Actualiza únicamente los datos de facturación de una Compra ya registrada.
    /// </summary>
    Task<Result<Compra, DomainError>> ActualizarDatosFacturaAsync(
        int id, string metodoPago, string numeroFacturaProveedor, string facturaAdjuntaUrl);

    /// <summary>
    /// Actualiza únicamente las notas de una Compra ya registrada.
    /// </summary>
    Task<Result<Compra, DomainError>> ActualizarNotasAsync(int id, string notas);

    /// <summary>
    /// Actualiza ÚNICAMENTE Descripcion/ImporteComprasExcepcionales de una Compra ya
    /// registrada, sin tocar Lineas ni ningún otro campo. Pensado para corregir a posteriori
    /// desde FacturaDetalle.razor, o para añadirlo si no se rellenó al confirmar la recepción.
    /// </summary>
    Task<Result<Compra, DomainError>> ActualizarComprasExcepcionalesAsync(int id, string descripcion, decimal importe);

    /// <summary>
    /// Actualiza ÚNICAMENTE PrecioUnitario de una LineaCompra ya registrada, sin tocar
    /// Cantidad, ProductoId ni ninguna otra línea de la misma Compra. Pensado para corregir
    /// a posteriori el precio real de una factura ya guardada (FacturaDetalle.razor).
    /// </summary>
    Task<Result<LineaCompra, DomainError>> ActualizarPrecioLineaAsync(int lineaCompraId, decimal nuevoPrecio);

    /// <summary>
    /// Actualiza ÚNICAMENTE Cantidad de una LineaCompra ya registrada, sin tocar
    /// PrecioUnitario, ProductoId ni ninguna otra línea de la misma Compra. Pensado para
    /// corregir a posteriori la cantidad realmente recibida de una factura ya guardada
    /// (FacturaDetalle.razor).
    /// </summary>
    Task<Result<LineaCompra, DomainError>> ActualizarCantidadLineaAsync(int lineaCompraId, int nuevaCantidad);
}
