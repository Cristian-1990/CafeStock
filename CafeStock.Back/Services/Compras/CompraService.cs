using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Compras;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
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

    public CompraService(ICompraRepository repository, IValidador<Compra> validador)
    {
        _repository = repository;
        _validador = validador;
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
            Log.Information("Compra registrada: Id={Id}, ProveedorId={ProveedorId}, Lineas={Lineas}",
                resultado.Value.Id, resultado.Value.ProveedorId, resultado.Value.Lineas.Count);
        return resultado;
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
}
