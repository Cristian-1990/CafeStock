using CafeStock.Back.Errors.Common;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Validators.Common;

public interface IValidador<T>
{
    /// <summary>
    /// Valida una entidad pudiendo devolver solo dos resultados posibles.
    /// </summary>
    /// <param name="entidad">Objeto Producto a validar</param>
    /// <returns>Devuelve un objeto o un error de dominio</returns>
    Result<T, DomainError> Validar(T entidad);
}