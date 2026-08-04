using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;
namespace CafeStock.Back.Repositories.Proveedores.Base;

public interface IProveedorRepository
{
    Task<IEnumerable<Proveedor>> GetAllAsync();
    Task<Result<Proveedor, DomainError>> GetByIdAsync(int id);
    Task<Result<Proveedor, DomainError>> CreateAsync(Proveedor proveedor);
    Task<Result<Proveedor, DomainError>> UpdateAsync(int id, Proveedor proveedor);
    Task<Result<Proveedor, DomainError>> DeleteAsync(int id);
}
