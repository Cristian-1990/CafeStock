using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Proveedores;
using CafeStock.Back.Validators.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Validators.Proveedores;

/// <summary>
/// Validador de proveedor
/// </summary>
public class ValidadorProveedor : IValidador<Proveedor>
{
    public Result<Proveedor, DomainError> Validar(Proveedor proveedor)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(proveedor.Nombre))
            errores.Add("El nombre no puede estar vacío");

        if (errores.Any())
            return Result.Failure<Proveedor, DomainError>(ProveedorErrors.Validation(errores));
        return Result.Success<Proveedor, DomainError>(proveedor);
    }
}
