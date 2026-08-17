using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
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
    private bool _initialized;

    public RegistrosCafeEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        using var context = CreateContext();
        await context.EnsureCreatedAsync();
        // Se asegura también la columna SeguimientoIndividual (y no solo su propia tabla)
        // porque RegistrarVentaAsync lee y escribe ProductoEntity en el mismo contexto.
        await context.AsegurarColumnaSeguimientoIndividualAsync();
        await context.AsegurarTablaRegistrosCafeAsync();
        _initialized = true;
    }

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
        using var context = CreateContext();
        var producto = await context.Productos.FindAsync(productoId);
        if (producto is null)
            return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.ProductoNoEncontrado(productoId));
        if (producto.StockActual <= 0)
            return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.SinStock(productoId));

        try
        {
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
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al registrar la venta del producto {ProductoId}", productoId);
            return Result.Failure<RegistroCafe, DomainError>(RegistroCafeErrors.DatabaseError(ex.Message));
        }
    }
}
