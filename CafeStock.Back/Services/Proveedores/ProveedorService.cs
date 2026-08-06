using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Validators.Common;
using Serilog;

namespace CafeStock.Back.Services.Proveedores;

public class ProveedorService : IProveedorService
{
    private readonly IProveedorRepository _repository;
    private readonly IValidador<Proveedor> _validador;

    public ProveedorService(IProveedorRepository repository, IValidador<Proveedor> validador)
    {
        _repository = repository;
        _validador = validador;
    }

    public async Task<IEnumerable<Proveedor>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Result<Proveedor, DomainError>> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Result<Proveedor, DomainError>> CreateAsync(Proveedor proveedor)
    {
        var validacion = _validador.Validar(proveedor);
        if (validacion.IsFailure)
            return validacion;

        var resultado = await _repository.CreateAsync(proveedor);
        if (resultado.IsSuccess)
            Log.Information("Proveedor creado: Id={Id}, Nombre={Nombre}", resultado.Value.Id, resultado.Value.Nombre);
        return resultado;
    }

    public async Task<Result<Proveedor, DomainError>> UpdateAsync(int id, Proveedor proveedor)
    {
        var validacion = _validador.Validar(proveedor);
        if (validacion.IsFailure)
            return validacion;

        var resultado = await _repository.UpdateAsync(id, proveedor);
        if (resultado.IsSuccess)
            Log.Information("Proveedor editado: Id={Id}, Nombre={Nombre}", id, proveedor.Nombre);
        return resultado;
    }

    public async Task<Result<Proveedor, DomainError>> DeleteAsync(int id)
    {
        var resultado = await _repository.DeleteAsync(id);
        if (resultado.IsSuccess)
            Log.Information("Proveedor eliminado: Id={Id}", id);
        return resultado;
    }
}
