using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Errors.RegistrosCafe;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
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
                var producto = await context.Productos.FindAsync(productoId);
                if (producto is null)
                    return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.ProductoNoEncontrado(productoId));
                if (producto.StockActual <= 0)
                    return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.SinStock(productoId));

                producto.StockActual -= 1;

                var entity = new RegistroCafeEntity
                {
                    ProductoId = productoId,
                    NombreTrabajador = nombreTrabajador,
                    Fecha = DateTime.Now
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
}
