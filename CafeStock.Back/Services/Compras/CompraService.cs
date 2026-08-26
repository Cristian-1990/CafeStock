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

    /// <summary>
    /// Corrige el precio de una línea ya registrada. La recencia (qué Compra es la más
    /// reciente entre las que incluyen ese producto) se calcula en memoria sobre
    /// GetAllAsync(), igual que ya hacen los informes — el volumen de datos de una cafetería
    /// es pequeño y así la lógica de negocio queda en el servicio, no repartida en SQL.
    /// </summary>
    public async Task<Result<LineaCompra, DomainError>> ActualizarPrecioLineaAsync(int lineaCompraId, decimal nuevoPrecio)
    {
        if (nuevoPrecio <= 0)
            return Result.Failure<LineaCompra, DomainError>(
                CompraErrors.Validation(["El precio unitario debe ser mayor que 0"]));

        var todasLasCompras = await _repository.GetAllAsync();
        var compraDeLaLinea = todasLasCompras.FirstOrDefault(c => c.Lineas.Any(l => l.Id == lineaCompraId));
        if (compraDeLaLinea is null)
            return Result.Failure<LineaCompra, DomainError>(CompraErrors.LineaNotFound(lineaCompraId));

        var linea = compraDeLaLinea.Lineas.First(l => l.Id == lineaCompraId);
        var precioAnterior = linea.PrecioUnitario;

        var resultado = await _repository.ActualizarPrecioLineaAsync(lineaCompraId, nuevoPrecio);
        if (resultado.IsFailure)
            return resultado;

        var compraMasRecienteDelProducto = todasLasCompras
            .Where(c => c.Lineas.Any(l => l.ProductoId == linea.ProductoId))
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .First();
        var esLaMasReciente = compraMasRecienteDelProducto.Id == compraDeLaLinea.Id;

        var referenciaActualizada = false;
        if (esLaMasReciente)
        {
            var actualizacionProducto = await _productoService.ActualizarPrecioUnitarioAsync(linea.ProductoId, nuevoPrecio);
            referenciaActualizada = actualizacionProducto.IsSuccess;
            if (!actualizacionProducto.IsSuccess)
                Log.Error(
                    "No se pudo sincronizar la referencia de precio del producto {ProductoId} tras corregir la línea {LineaCompraId}: {Error}",
                    linea.ProductoId, lineaCompraId, actualizacionProducto.Error.Message);
        }

        Log.Information(
            "Precio de línea de compra corregido: LineaCompraId={LineaCompraId}, ProductoId={ProductoId}, PrecioAnterior={PrecioAnterior}, PrecioNuevo={PrecioNuevo}, ReferenciaActualizada={ReferenciaActualizada}",
            lineaCompraId, linea.ProductoId, precioAnterior, nuevoPrecio, referenciaActualizada);

        return resultado;
    }
}
