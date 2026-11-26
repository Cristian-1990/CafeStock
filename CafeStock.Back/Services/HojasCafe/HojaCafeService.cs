using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.HojasCafe;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.HojasCafe.Base;
using CafeStock.Back.Services.Productos;
using CSharpFunctionalExtensions;
using Serilog;

namespace CafeStock.Back.Services.HojasCafe;

public class HojaCafeService : IHojaCafeService
{
    private readonly IHojaCafeRepository _repository;
    private readonly IProductoService _productos;

    public HojaCafeService(IHojaCafeRepository repository, IProductoService productos)
    {
        _repository = repository;
        _productos = productos;
    }

    public Task<HojaCafe> GetActualAsync() => _repository.GetActualAsync();

    public Task<IReadOnlyList<ResumenHojaCafe>> GetResumenesAsync() => _repository.GetResumenesAsync();

    public Task<Result<HojaCafeDetalle, DomainError>> GetDetalleAsync(int? hojaId = null) => _repository.GetDetalleAsync(hojaId);

    public async Task<Result<HojaCafe, DomainError>> RecepcionarAsync(IReadOnlyList<NuevoPaqueteCafe> paquetes, string recepcionadoPor)
    {
        var errores = new List<string>();
        if (paquetes.Count == 0)
            errores.Add("Añade al menos un paquete");
        if (string.IsNullOrWhiteSpace(recepcionadoPor))
            errores.Add("Falta quién recepciona");
        if (paquetes.Any(p => p.FechaTueste is { } f && f.Date > DateTime.Today))
            errores.Add("La fecha de tueste no puede ser futura");
        if (errores.Count > 0)
            return Result.Failure<HojaCafe, DomainError>(HojaCafeErrors.Validation(errores));

        var clasificacion = await ClasificarCafesNuevosAsync(paquetes);
        if (clasificacion.IsFailure)
            return Result.Failure<HojaCafe, DomainError>(clasificacion.Error);

        var resultado = await _repository.RecepcionarAsync(paquetes, recepcionadoPor.Trim());
        if (resultado.IsSuccess)
            Log.Information("Café recepcionado: {Paquetes} paquetes, hoja {HojaId}, por {Trabajador}",
                paquetes.Count, resultado.Value.Id, recepcionadoPor);
        return resultado;
    }

    /// <summary>
    /// Los cafés SinClasificar reciben su grupo y formato al llegar (los de los paquetes que se recepcionan). Pasa por
    /// ProductoService, así que se aplican la validación y la rotación del especial (no entra un especial nuevo mientras
    /// al anterior del mismo formato le queden paquetes). El stock máximo sube a 1 si estaba a 0: un café del tablero lo necesita.
    /// </summary>
    private async Task<Result<bool, DomainError>> ClasificarCafesNuevosAsync(IReadOnlyList<NuevoPaqueteCafe> paquetes)
    {
        foreach (var porProducto in paquetes.GroupBy(p => p.ProductoId))
        {
            var lectura = await _productos.GetByIdAsync(porProducto.Key);
            if (lectura.IsFailure || lectura.Value.GrupoCafe != GrupoCafe.SinClasificar)
                continue; // si no existe lo dirá la recepción; si ya está clasificado no se toca

            var producto = lectura.Value;
            var pedido = porProducto.First();
            if (porProducto.Any(p => p.Grupo != pedido.Grupo || p.Formato != pedido.Formato))
                return Result.Failure<bool, DomainError>(HojaCafeErrors.Validation(
                    [$"{producto.Nombre}: todos los paquetes de un café sin clasificar llevan el mismo grupo y formato"]));
            if (pedido.Grupo is not { } grupo || !grupo.EsDelTablero() || pedido.Formato is not { } formato || formato == FormatoCafe.SinFormato)
                return Result.Failure<bool, DomainError>(HojaCafeErrors.Validation(
                    [$"{producto.Nombre} aún no está clasificado: elige su grupo y su formato"]));

            var guardado = await _productos.UpdateAsync(producto.Id, producto with
            {
                GrupoCafe = grupo,
                FormatoCafe = formato,
                StockMaximo = Math.Max(producto.StockMaximo, 1m),
                StockActualAlLeer = producto.StockActual
            });
            if (guardado.IsFailure)
                return Result.Failure<bool, DomainError>(guardado.Error);
        }
        return Result.Success<bool, DomainError>(true);
    }
}
