using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.RegistrosCafe;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.RegistrosCafe.Base;
using CafeStock.Back.Services.Productos;
using Serilog;

namespace CafeStock.Back.Services.RegistrosCafe;

public class RegistroCafeService : IRegistroCafeService
{
    private readonly IRegistroCafeRepository _repository;
    private readonly IProductoService _productoService;

    public RegistroCafeService(IRegistroCafeRepository repository, IProductoService productoService)
    {
        _repository = repository;
        _productoService = productoService;
    }

    public async Task<IEnumerable<RegistroCafe>> GetUltimosAsync(int cantidad = 20)
    {
        var registros = await _repository.GetUltimosAsync(cantidad);
        var productosPorId = (await _productoService.GetAllAsync()).ToDictionary(p => p.Id);

        return registros
            .Select(r => r with
            {
                ProductoNombre = productosPorId.TryGetValue(r.ProductoId, out var producto) ? producto.Nombre : null
            })
            .ToList();
    }

    public async Task<Result<RegistroCafe, DomainError>> RegistrarVentaAsync(int productoId, string nombreTrabajador)
    {
        if (string.IsNullOrWhiteSpace(nombreTrabajador))
            return Result.Failure<RegistroCafe, DomainError>(
                RegistroCafeErrors.Validation(["El nombre del trabajador es obligatorio"]));

        var resultado = await _repository.RegistrarVentaAsync(productoId, nombreTrabajador.Trim());
        if (resultado.IsSuccess)
            Log.Information("Venta registrada: ProductoId={ProductoId}, NombreTrabajador={NombreTrabajador}",
                productoId, resultado.Value.NombreTrabajador);
        return resultado;
    }
}
