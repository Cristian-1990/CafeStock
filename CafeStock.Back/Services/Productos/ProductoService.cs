using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Productos;
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

        var rotacion = await ComprobarRotacionDelEspecialAsync(null, producto);
        if (rotacion.IsFailure)
            return rotacion;

        // Un producto con grupo de café se cuenta de uno en uno, como en Venta.
        if (producto.GrupoCafe != GrupoCafe.SinGrupo)
            producto = producto with { SeguimientoIndividual = true };

        var resultado = await _repository.CreateAsync(producto);
        if (resultado.IsSuccess)
        {
            Log.Information("Producto creado: Id={Id}, Nombre={Nombre}", resultado.Value.Id, resultado.Value.Nombre);
            await RetirarEspecialesAnterioresAsync(resultado.Value);
        }
        return resultado;
    }

    public async Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto)
    {
        var validacion = _validador.Validar(producto);
        if (validacion.IsFailure)
            return validacion;

        var rotacion = await ComprobarRotacionDelEspecialAsync(id, producto);
        if (rotacion.IsFailure)
            return rotacion;

        var resultado = await _repository.UpdateAsync(id, producto);
        if (resultado.IsSuccess)
        {
            Log.Information("Producto editado: Id={Id}, Nombre={Nombre}", id, producto.Nombre);
            await RetirarEspecialesAnterioresAsync(resultado.Value);
        }
        return resultado;
    }

    /// <summary>
    /// Regla de la rotación del café especial: solo hay uno por formato y un especial nuevo solo puede entrar
    /// cuando el anterior (del mismo formato: paquete o cuarto) se ha agotado. Se comprueba por formato porque
    /// el mismo café tiene su paquete y sus cuartos, que no se acaban a la vez.
    /// </summary>
    private async Task<Result<Producto, DomainError>> ComprobarRotacionDelEspecialAsync(int? id, Producto producto)
    {
        if (producto.GrupoCafe != GrupoCafe.Especial || producto.FormatoCafe == FormatoCafe.SinFormato)
            return Result.Success<Producto, DomainError>(producto);

        var conStock = (await EspecialesDelMismoFormatoAsync(id, producto))
            .Where(p => p.StockActual > 0)
            .ToList();
        if (conStock.Count == 0)
            return Result.Success<Producto, DomainError>(producto);

        var detalle = string.Join(", ", conStock.Select(p => $"«{p.Nombre}» (quedan {p.StockActual:0.##})"));
        return Result.Failure<Producto, DomainError>(ProductoErrors.Validation(
            [$"El especial actual aún tiene stock: {detalle}. Se agota primero y entonces puede entrar «{producto.Nombre}»."]));
    }

    /// <summary>
    /// Tras guardar un producto como especial, los especiales anteriores del mismo formato (ya agotados: lo
    /// comprobó ComprobarRotacionDelEspecialAsync) dejan de serlo, para que en el tablero solo salga el vigente.
    /// </summary>
    private async Task RetirarEspecialesAnterioresAsync(Producto guardado)
    {
        if (guardado.GrupoCafe != GrupoCafe.Especial || guardado.FormatoCafe == FormatoCafe.SinFormato)
            return;

        foreach (var anterior in await EspecialesDelMismoFormatoAsync(guardado.Id, guardado))
        {
            var retirado = await _repository.UpdateAsync(anterior.Id, anterior with { GrupoCafe = GrupoCafe.SinGrupo, FormatoCafe = FormatoCafe.SinFormato });
            if (retirado.IsSuccess)
                Log.Information("Especial retirado del tablero de café: Id={Id}, Nombre={Nombre}", anterior.Id, anterior.Nombre);
        }
    }

    private async Task<List<Producto>> EspecialesDelMismoFormatoAsync(int? id, Producto producto) =>
        (await _repository.GetAllAsync())
            .Where(p => p.Id != id && p.GrupoCafe == GrupoCafe.Especial && p.FormatoCafe == producto.FormatoCafe)
            .ToList();

    public async Task<Result<Producto, DomainError>> DeleteAsync(int id)
    {
        var resultado = await _repository.DeleteAsync(id);
        if (resultado.IsSuccess)
            Log.Information("Producto eliminado: Id={Id}", id);
        return resultado;
    }

    public async Task<Result<Producto, DomainError>> ConfirmarRecepcionAsync(int id, int cantidadRecibida)
    {
        if (cantidadRecibida <= 0)
            return Result.Failure<Producto, DomainError>(
                ProductoErrors.Validation(["La cantidad recibida debe ser mayor que 0"]));

        var resultado = await _repository.ConfirmarRecepcionAsync(id, cantidadRecibida);
        if (resultado.IsSuccess)
            Log.Information("Stock repuesto: Id={Id}, Nombre={Nombre}, CantidadRecibida={CantidadRecibida}",
                resultado.Value.Id, resultado.Value.Nombre, cantidadRecibida);
        return resultado;
    }

    public async Task<IEnumerable<Producto>> GetProductosBajoMinimoAsync()
    {
        return await _repository.ProductosUrgentes();
    }

    public async Task<Result<Producto, DomainError>> ActualizarStockActualAsync(int id, decimal nuevaCantidad)
    {
        if (nuevaCantidad < 0)
            return Result.Failure<Producto, DomainError>(
                ProductoErrors.Validation(["El stock no puede ser menor que 0"]));

        var resultado = await _repository.ActualizarStockActualAsync(id, nuevaCantidad);
        if (resultado.IsSuccess)
            Log.Information("Stock actual actualizado por recuento: Id={Id}, Nombre={Nombre}, NuevaCantidad={NuevaCantidad}",
                resultado.Value.Id, resultado.Value.Nombre, nuevaCantidad);
        return resultado;
    }

    public async Task<Result<Producto, DomainError>> ActualizarPrecioUnitarioAsync(int id, decimal nuevoPrecio)
    {
        if (nuevoPrecio <= 0)
            return Result.Failure<Producto, DomainError>(
                ProductoErrors.Validation(["El precio unitario debe ser mayor que 0"]));

        var resultado = await _repository.ActualizarPrecioUnitarioAsync(id, nuevoPrecio);
        if (resultado.IsSuccess)
            Log.Information("Precio unitario actualizado: Id={Id}, Nombre={Nombre}, NuevoPrecio={NuevoPrecio}",
                resultado.Value.Id, resultado.Value.Nombre, nuevoPrecio);
        return resultado;
    }

    public async Task<Result<Producto, DomainError>> AjustarStockAsync(int id, int delta)
    {
        var resultado = await _repository.AjustarStockActualAsync(id, delta);
        if (resultado.IsSuccess)
            Log.Information("Stock ajustado: Id={Id}, Nombre={Nombre}, Delta={Delta}, StockResultante={StockResultante}",
                resultado.Value.Id, resultado.Value.Nombre, delta, resultado.Value.StockActual);
        return resultado;
    }
}