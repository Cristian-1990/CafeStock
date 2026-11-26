
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
        // El stock admite decimales (p.ej. 0,5 de un paquete abierto a la mitad) pero solo hasta dos.
        if (Math.Round(producto.StockActual, 2) != producto.StockActual || Math.Round(producto.StockMaximo, 2) != producto.StockMaximo)
            errores.Add("El stock admite como máximo dos decimales");
        // Grupo y formato del café van juntos: o los dos o ninguno (si no, el producto no saldría en el tablero).
        if ((producto.GrupoCafe != GrupoCafe.SinGrupo) != (producto.FormatoCafe != FormatoCafe.SinFormato))
            errores.Add("El grupo y el formato de café van juntos: elige los dos o ninguno");
        if(producto.StockMaximo < producto.StockActual)
            errores.Add("El stock actual no puede ser mayor que el stock máximo.");
        if(producto.PrecioUnitario < 0)
            errores.Add("El precio unitario no puede ser negativo");
        if (errores.Any())
            return Result.Failure<Producto, DomainError>(ProductoErrors.Validation(errores));
        return Result.Success<Producto, DomainError>(producto);
    }
}