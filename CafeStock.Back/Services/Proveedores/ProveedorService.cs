using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Validators.Common;

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

        return await _repository.CreateAsync(proveedor);
    }

    public async Task<Result<Proveedor, DomainError>> UpdateAsync(int id, Proveedor proveedor)
    {
        var validacion = _validador.Validar(proveedor);
        if (validacion.IsFailure)
            return validacion;

        return await _repository.UpdateAsync(id, proveedor);
    }

    public async Task<Result<Proveedor, DomainError>> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
}
