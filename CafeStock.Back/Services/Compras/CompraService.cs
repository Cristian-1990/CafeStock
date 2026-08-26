using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Compras;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Validators.Common;
using Serilog;

namespace CafeStock.Back.Services.Compras;

public class CompraService : ICompraService
{
    /// <summary>
    /// Únicos valores admitidos para MetodoPago; vacío también es válido (no especificado).
    /// </summary>
    private static readonly string[] MetodosPagoValidos = ["Efectivo", "Transferencia", "Domiciliado"];

    private readonly ICompraRepository _repository;
    private readonly IValidador<Compra> _validador;
    private readonly IProductoService _productoService;

    public CompraService(ICompraRepository repository, IValidador<Compra> validador, IProductoService productoService)
    {
        _repository = repository;
        _validador = validador;
        _productoService = productoService;
    }

    public async Task<IEnumerable<Compra>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Result<Compra, DomainError>> CreateAsync(Compra compra)
    {
        var validacion = _validador.Validar(compra);
        if (validacion.IsFailure)
            return validacion;

        var resultado = await _repository.CreateAsync(compra);
        if (resultado.IsSuccess)
        {
            Log.Information("Compra registrada: Id={Id}, ProveedorId={ProveedorId}, Lineas={Lineas}",
                resultado.Value.Id, resultado.Value.ProveedorId, resultado.Value.Lineas.Count);
            await SincronizarPreciosReferenciaAsync(resultado.Value.Lineas);
        }
        return resultado;
    }

    /// <summary>
    /// Tras registrar una compra, sincroniza Producto.PrecioUnitario (precio de referencia)
    /// con el precio realmente pagado en cada línea, cuando difiere. La compra ya quedó
    /// guardada en este punto, así que es best-effort: un fallo al sincronizar una línea se
    /// loguea y no debe tumbar la recepción ni bloquear la sincronización de las demás líneas.
    /// </summary>
    private async Task SincronizarPreciosReferenciaAsync(IReadOnlyCollection<LineaCompra> lineas)
    {
        foreach (var linea in lineas)
        {
            try
            {
                if (linea.PrecioUnitario <= 0)
                    continue;

                var producto = await _productoService.GetByIdAsync(linea.ProductoId);
                if (producto.IsFailure || producto.Value.PrecioUnitario == linea.PrecioUnitario)
                    continue;

                var precioAnterior = producto.Value.PrecioUnitario;
                var actualizacion = await _productoService.ActualizarPrecioUnitarioAsync(linea.ProductoId, linea.PrecioUnitario);
                if (actualizacion.IsSuccess)
                    Log.Information(
                        "Precio de referencia sincronizado tras compra: ProductoId={ProductoId}, PrecioAnterior={PrecioAnterior}, PrecioNuevo={PrecioNuevo}",
                        linea.ProductoId, precioAnterior, linea.PrecioUnitario);
                else
                    Log.Error(
                        "No se pudo sincronizar el precio de referencia del producto {ProductoId} tras la compra: {Error}",
                        linea.ProductoId, actualizacion.Error.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al sincronizar el precio de referencia del producto {ProductoId} tras la compra", linea.ProductoId);
            }
        }
    }

    public async Task<Result<Compra, DomainError>> ActualizarDatosFacturaAsync(
        int id, string metodoPago, string numeroFacturaProveedor, string facturaAdjuntaUrl)
    {
        if (!string.IsNullOrWhiteSpace(metodoPago) && !MetodosPagoValidos.Contains(metodoPago))
            return Result.Failure<Compra, DomainError>(
                CompraErrors.Validation([$"Método de pago no válido: {metodoPago}"]));

        var resultado = await _repository.ActualizarDatosFacturaAsync(
            id, metodoPago, numeroFacturaProveedor, facturaAdjuntaUrl);
        if (resultado.IsSuccess)
            Log.Information("Datos de facturación actualizados: Id={Id}, MetodoPago={MetodoPago}, NumeroFacturaProveedor={NumeroFacturaProveedor}",
                id, metodoPago, numeroFacturaProveedor);
        return resultado;
    }

    public async Task<Result<Compra, DomainError>> ActualizarNotasAsync(int id, string notas)
    {
        var resultado = await _repository.ActualizarNotasAsync(id, notas);
        if (resultado.IsSuccess)
            Log.Information("Notas actualizadas: Id={Id}", id);
        return resultado;
    }
}
