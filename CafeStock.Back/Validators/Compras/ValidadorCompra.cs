using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Compras;
using CafeStock.Back.Models;
using CafeStock.Back.Validators.Common;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Validators.Compras;

/// <summary>
/// Validador de compra: segunda barrera de defensa además de lo que ya filtra la UI
/// (cantidad y precio unitario por línea antes de confirmar).
/// </summary>
public class ValidadorCompra : IValidador<Compra>
{
    public Result<Compra, DomainError> Validar(Compra compra)
    {
        var errores = new List<string>();

        if (!compra.Lineas.Any())
            errores.Add("La compra debe tener al menos una línea");

        foreach (var linea in compra.Lineas)
        {
            if (linea.Cantidad <= 0)
                errores.Add($"La cantidad de la línea del producto {linea.ProductoId} debe ser mayor que 0");
            if (linea.PrecioUnitario <= 0)
                errores.Add($"El precio unitario de la línea del producto {linea.ProductoId} debe ser mayor que 0");
        }

        if (errores.Any())
            return Result.Failure<Compra, DomainError>(CompraErrors.Validation(errores));
        return Result.Success<Compra, DomainError>(compra);
    }
}
