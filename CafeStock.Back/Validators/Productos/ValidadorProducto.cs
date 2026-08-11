
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Productos;
using CafeStock.Back.Validators.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Validators.Productos;

/// <summary>
/// Validador de producto, d
/// </summary>
public class ValidadorProducto : IValidador<Producto>
{
    public Result<Producto, DomainError> Validar(Producto producto)
    {
        var errores = new List<string>(); //Lista que acumuka errores
        
        if(string.IsNullOrWhiteSpace(producto.Nombre))
            errores.Add("El nombre no puede estar vacío");
        if(producto.StockActual < 0)
            errores.Add("El stock no puede ser menor que 0");
        if(producto.StockMaximo <= 0)
            errores.Add("El stock máximo debe ser mayor que 0");
        if(producto.StockMaximo < producto.StockActual)
            errores.Add("El stock actual no puede ser mayor que el stock máximo.");
        if(producto.PrecioUnitario < 0)
            errores.Add("El precio unitario no puede ser negativo");
        if (errores.Any())
            return Result.Failure<Producto, DomainError>(ProductoErrors.Validation(errores));
        return Result.Success<Producto, DomainError>(producto);
    }
}