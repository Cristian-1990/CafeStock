using CafeStock.Back.Entity;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeStock.Back.Repositories.HojasCafe.EfCore;

/// <summary>
/// Lo que comparten quien registra un uso y quien recepciona: tener siempre una hoja abierta y que los cafés con
/// stock anterior a las hojas tengan ya sus paquetes. Es idempotente: no hace nada si todo está en su sitio.
/// </summary>
internal static class HojaCafeSembrado
{
    public const string OrigenInicial = "Hoja inicial";

    public static async Task<HojaCafeEntity> AsegurarHojaActualAsync(AppDbContext context)
    {
        var actual = await context.HojasCafe
            .Where(h => h.FechaCierre == null)
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync();

        if (actual is null)
        {
            actual = new HojaCafeEntity { FechaApertura = DateTime.Now };
            context.HojasCafe.Add(actual);
            await context.SaveChangesAsync();
        }

        await SembrarPaquetesAsync(context, actual);
        return actual;
    }

    /// <summary>
    /// Para cada café con grupo y formato que aún no tiene ningún paquete: un paquete libre por cada unidad entera de
    /// su stock y un paquete usado por cada uso que ya tenía registrado (con su nombre y fecha), todos en la hoja
    /// actual. Es lo mismo que dibujaba antes el tablero a partir del stock, ahora guardado.
    /// </summary>
    private static async Task SembrarPaquetesAsync(AppDbContext context, HojaCafeEntity hoja)
    {
        var conPaquetes = await context.PaquetesCafe.Select(p => p.ProductoId).Distinct().ToListAsync();
        var candidatos = (await context.Productos
                .Where(p => p.FormatoCafe != FormatoCafe.SinFormato)
                .ToListAsync())
            .Where(p => p.GrupoCafe.EsDelTablero() && !conPaquetes.Contains(p.Id))
            .ToList();
        if (candidatos.Count == 0) return;

        var ahora = DateTime.Now;
        foreach (var entidad in candidatos)
        {
            var producto = entidad.ToProducto();
            var registros = await context.RegistrosCafe
                .Where(r => r.ProductoId == entidad.Id)
                .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Id)
                .Take(TableroCafe.TotalHuecos(producto))
                .ToListAsync();

            var huecos = TableroCafe.Huecos(producto, registros.Select(r => r.ToRegistroCafe()).ToList());
            foreach (var hueco in huecos)
            {
                if (hueco.Usado && hueco.RegistroId is null) continue; // usado sin registro: no hay de quién ni cuándo
                var registro = hueco.RegistroId is int id ? registros.First(r => r.Id == id) : null;
                var paquete = new PaqueteCafeEntity
                {
                    ProductoId = entidad.Id,
                    Grupo = (int)entidad.GrupoCafe,
                    Formato = (int)entidad.FormatoCafe,
                    HojaOrigen = hoja,
                    RecepcionadoPor = OrigenInicial,
                    FechaRecepcion = registro?.Fecha ?? ahora
                };
                context.PaquetesCafe.Add(paquete);
                if (registro is not null)
                {
                    registro.Paquete = paquete;
                    registro.HojaId = hoja.Id;
                }
            }
        }
        await context.SaveChangesAsync();
    }
}
