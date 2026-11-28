using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Errors.RegistrosCafe;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.HojasCafe.EfCore;
using CafeStock.Back.Repositories.RegistrosCafe.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.RegistrosCafe.EfCore;

public class RegistrosCafeEfRepository : IRegistroCafeRepository
{
    private readonly string _connectionString;

    public RegistrosCafeEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    // El esquema lo gestiona DatabaseInitializer (EF Core Migrations); es idempotente y se
    // ejecuta una sola vez por base de datos aunque lo llamen los seis repositorios.
    private Task InitializeAsync() => DatabaseInitializer.EnsureInitializedAsync(_connectionString);

    public async Task<IEnumerable<RegistroCafe>> GetUltimosAsync(int cantidad)
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.RegistrosCafe
            .OrderByDescending(r => r.Fecha)
            .Take(cantidad)
            .Select(e => e.ToRegistroCafe())
            .ToListAsync();
    }

    public async Task<Result<RegistroCafe, DomainError>> RegistrarVentaAsync(int productoId, string nombreTrabajador)
    {
        await InitializeAsync();
        try
        {
            // Concurrencia optimista: StockActual es un token de concurrencia (ver AppDbContext),
            // así que si otra tablet vendió el mismo producto entre nuestra lectura y nuestro
            // guardado, EF lanza DbUpdateConcurrencyException y se repite desde la lectura
            // con el stock ya actualizado (puede acabar en SinStock, que es lo correcto).
            return await ReintentoConcurrencia.EjecutarAsync(async () =>
            {
                using var context = CreateContext();
                // La hoja abierta y los paquetes de los cafés con stock anterior a las hojas: antes de tocar el stock.
                var hoja = await HojaCafeSembrado.AsegurarHojaActualAsync(context);

                var producto = await context.Productos.FindAsync(productoId);
                if (producto is null)
                    return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.ProductoNoEncontrado(productoId));
                if (producto.StockActual <= 0)
                    return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.SinStock(productoId));

                producto.StockActual -= 1;

                // Se usa el paquete libre de tueste más antiguo (los sin fecha, antes: no se sabe cuánto llevan). Es el
                // que el mapa pinta al final. Si el café no tiene paquetes (stock sin repartir), el uso se registra
                // igualmente, solo ligado a la hoja.
                var paquete = await context.PaquetesCafe
                    .Where(p => p.ProductoId == productoId && !context.RegistrosCafe.Any(r => r.PaqueteId == p.Id))
                    .OrderBy(p => p.FechaTueste == null ? 0 : 1).ThenBy(p => p.FechaTueste)
                    .ThenBy(p => p.HojaOrigenId).ThenBy(p => p.Id)
                    .FirstOrDefaultAsync();

                var entity = new RegistroCafeEntity
                {
                    ProductoId = productoId,
                    NombreTrabajador = nombreTrabajador,
                    Fecha = DateTime.Now,
                    HojaId = hoja.Id,
                    PaqueteId = paquete?.Id
                };
                context.RegistrosCafe.Add(entity);

                await context.SaveChangesAsync();
                return Result.Success<RegistroCafe, DomainError>(entity.ToRegistroCafe());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al registrar la venta del producto {ProductoId}", productoId);
            return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<IReadOnlyDictionary<int, List<RegistroCafe>>> GetUltimosPorProductoAsync(IReadOnlyCollection<int> productoIds, int porProducto)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var resultado = new Dictionary<int, List<RegistroCafe>>();
        foreach (var productoId in productoIds)
        {
            var registros = await context.RegistrosCafe
                .Where(r => r.ProductoId == productoId)
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Id)
                .Take(porProducto)
                .Select(e => e.ToRegistroCafe())
                .ToListAsync();
            if (registros.Count > 0)
                resultado[productoId] = registros;
        }
        return resultado;
    }

    public async Task<Result<RegistroCafe, DomainError>> DeshacerVentaAsync(int registroId)
    {
        await InitializeAsync();
        try
        {
            // Misma concurrencia optimista que RegistrarVentaAsync: StockActual es token de concurrencia.
            return await ReintentoConcurrencia.EjecutarAsync(async () =>
            {
                using var context = CreateContext();
                var registro = await context.RegistrosCafe.FindAsync(registroId);
                if (registro is null)
                    return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.RegistroNoEncontrado(registroId));

                var producto = await context.Productos.FindAsync(registro.ProductoId);
                if (producto is not null)
                    producto.StockActual += 1;

                context.RegistrosCafe.Remove(registro);
                await context.SaveChangesAsync();
                return Result.Success<RegistroCafe, DomainError>(registro.ToRegistroCafe());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al deshacer el registro de café {RegistroId}", registroId);
            return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.DatabaseError(ex.Message));
        }
    }
}
