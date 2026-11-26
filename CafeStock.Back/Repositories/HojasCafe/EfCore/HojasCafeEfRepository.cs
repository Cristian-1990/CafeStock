using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.HojasCafe;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.HojasCafe.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.HojasCafe.EfCore;

public class HojasCafeEfRepository : IHojaCafeRepository
{
    private readonly string _connectionString;

    public HojasCafeEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    private Task InitializeAsync() => DatabaseInitializer.EnsureInitializedAsync(_connectionString);

    private static HojaCafe ToModel(HojaCafeEntity e) =>
        new() { Id = e.Id, FechaApertura = e.FechaApertura, FechaCierre = e.FechaCierre };

    private static PaqueteCafe ToModel(PaqueteCafeEntity e) => new()
    {
        Id = e.Id,
        ProductoId = e.ProductoId,
        ProductoNombre = e.Producto?.Nombre ?? string.Empty,
        Grupo = (GrupoCafe)e.Grupo,
        Formato = (FormatoCafe)e.Formato,
        HojaOrigenId = e.HojaOrigenId,
        FechaTueste = e.FechaTueste,
        RecepcionadoPor = e.RecepcionadoPor,
        FechaRecepcion = e.FechaRecepcion
    };

    public async Task<HojaCafe> GetActualAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return ToModel(await HojaCafeSembrado.AsegurarHojaActualAsync(context));
    }

    public async Task<IReadOnlyList<ResumenHojaCafe>> GetResumenesAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        await HojaCafeSembrado.AsegurarHojaActualAsync(context);

        var hojas = await context.HojasCafe.OrderByDescending(h => h.Id).ToListAsync();
        var paquetes = await context.PaquetesCafe.Select(p => new { p.Id, p.HojaOrigenId }).ToListAsync();
        var usos = await context.RegistrosCafe.Where(r => r.PaqueteId != null).Select(r => new { r.PaqueteId, r.HojaId }).ToListAsync();

        return hojas.Select(h => new ResumenHojaCafe(
                ToModel(h),
                // Un paquete está en la hoja si llegó en ella o antes y no se usó antes de ella.
                paquetes.Count(p => p.HojaOrigenId <= h.Id && !usos.Any(u => u.PaqueteId == p.Id && u.HojaId < h.Id)),
                usos.Count(u => u.HojaId == h.Id)))
            .ToList();
    }

    public async Task<Result<HojaCafeDetalle, DomainError>> GetDetalleAsync(int? hojaId)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            var actual = await HojaCafeSembrado.AsegurarHojaActualAsync(context);
            var hoja = hojaId is null ? actual : await context.HojasCafe.FindAsync(hojaId.Value);
            if (hoja is null)
                return Result.Failure<HojaCafeDetalle, DomainError>(HojaCafeErrors.HojaNoEncontrada(hojaId!.Value));

            var paquetes = (await context.PaquetesCafe
                    .Include(p => p.Producto)
                    .Where(p => p.HojaOrigenId <= hoja.Id)
                    .ToListAsync())
                .Select(ToModel)
                .ToList();
            var usos = (await context.RegistrosCafe
                    .Where(r => r.PaqueteId != null && r.Paquete!.HojaOrigenId <= hoja.Id)
                    .ToListAsync())
                .ToDictionary(r => r.PaqueteId!.Value, r => r.ToRegistroCafe());

            return Result.Success<HojaCafeDetalle, DomainError>(
                new HojaCafeDetalle(ToModel(hoja), TableroCafe.ConstruirHoja(hoja.Id, paquetes, usos)));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al leer la hoja de café {HojaId}", hojaId);
            return Result.Failure<HojaCafeDetalle, DomainError>(HojaCafeErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<HojaCafe, DomainError>> RecepcionarAsync(IReadOnlyList<NuevoPaqueteCafe> paquetes, string recepcionadoPor)
    {
        await InitializeAsync();
        try
        {
            // StockActual es token de concurrencia: si otra tablet vendió mientras tanto, se repite desde la lectura.
            return await ReintentoConcurrencia.EjecutarAsync(async () =>
            {
                using var context = CreateContext();
                var actual = await HojaCafeSembrado.AsegurarHojaActualAsync(context);

                var ids = paquetes.Select(p => p.ProductoId).Distinct().ToList();
                var productos = await context.Productos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
                foreach (var id in ids)
                {
                    if (!productos.TryGetValue(id, out var producto))
                        return Result.Failure<HojaCafe, DomainError>(HojaCafeErrors.Validation(["Uno de los cafés ya no existe"]));
                    if (!producto.GrupoCafe.EsDelTablero() || producto.FormatoCafe == FormatoCafe.SinFormato)
                        return Result.Failure<HojaCafe, DomainError>(HojaCafeErrors.Validation(
                            [$"{producto.Nombre} no tiene grupo y formato de café: se asignan en su ficha"]));
                }

                // Si la hoja actual no tiene nada (ni paquetes ni usos) se reutiliza: no se llena el historial de hojas vacías.
                var tieneContenido = await context.PaquetesCafe.AnyAsync(p => p.HojaOrigenId == actual.Id)
                                     || await context.RegistrosCafe.AnyAsync(r => r.HojaId == actual.Id);
                var ahora = DateTime.Now;
                var hoja = actual;
                if (tieneContenido)
                {
                    actual.FechaCierre = ahora;
                    hoja = new HojaCafeEntity { FechaApertura = ahora };
                    context.HojasCafe.Add(hoja);
                }

                foreach (var nuevo in paquetes)
                {
                    var producto = productos[nuevo.ProductoId];
                    context.PaquetesCafe.Add(new PaqueteCafeEntity
                    {
                        ProductoId = producto.Id,
                        Grupo = (int)producto.GrupoCafe,
                        Formato = (int)producto.FormatoCafe,
                        HojaOrigen = hoja,
                        FechaTueste = nuevo.FechaTueste?.Date,
                        RecepcionadoPor = recepcionadoPor,
                        FechaRecepcion = ahora
                    });
                    producto.StockActual += 1;
                }

                await context.SaveChangesAsync();
                return Result.Success<HojaCafe, DomainError>(ToModel(hoja));
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al recepcionar café");
            return Result.Failure<HojaCafe, DomainError>(HojaCafeErrors.DatabaseError(ex.Message));
        }
    }
}
