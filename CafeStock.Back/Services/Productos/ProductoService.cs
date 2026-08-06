using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Validators.Common;
using Serilog;

namespace CafeStock.Back.Services.Productos;

public class ProductoService : IProductoService
{
    private readonly IProductoRepository _repository;
    private readonly IValidador<Producto> _validador;

    public ProductoService(IProductoRepository repository, IValidador<Producto> validador)
    {
        _repository = repository;
        _validador = validador;
    }

    public async Task<IEnumerable<Producto>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Result<Producto, DomainError>> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Result<Producto, DomainError>> CreateAsync(Producto producto)
    {
        var validacion = _validador.Validar(producto);
        if (validacion.IsFailure)
            return validacion;

        var resultado = await _repository.CreateAsync(producto);
        if (resultado.IsSuccess)
            Log.Information("Producto creado: Id={Id}, Nombre={Nombre}", resultado.Value.Id, resultado.Value.Nombre);
        return resultado;
    }

    public async Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto)
    {
        var validacion = _validador.Validar(producto);
        if (validacion.IsFailure)
            return validacion;

        var resultado = await _repository.UpdateAsync(id, producto);
        if (resultado.IsSuccess)
            Log.Information("Producto editado: Id={Id}, Nombre={Nombre}", id, producto.Nombre);
        return resultado;
    }

    public async Task<Result<Producto, DomainError>> DeleteAsync(int id)
    {
        var resultado = await _repository.DeleteAsync(id);
        if (resultado.IsSuccess)
            Log.Information("Producto eliminado: Id={Id}", id);
        return resultado;
    }

    public async Task<IEnumerable<Producto>> GetProductosBajoMinimoAsync()
    {
        return await _repository.ProductosUrgentes();
    }
}