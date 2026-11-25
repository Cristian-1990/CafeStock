using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Errors.Productos;
using CafeStock.Back.Models;
using CafeStock.Back.Mappers;
using CafeStock.Back.Repositories.Productos.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.Productos.EfCore;

public class ProductosEfRepository : IProductoRepository
{
    private readonly string _connectionString;

    public ProductosEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    // El esquema lo gestiona DatabaseInitializer (EF Core Migrations); es idempotente y se
    // ejecuta una sola vez por base de datos aunque lo llamen los seis repositorios.
    private Task InitializeAsync() => DatabaseInitializer.EnsureInitializedAsync(_connectionString);
    
    
    
    public async Task<IEnumerable<Producto>> GetAllAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Productos
            .Select(e => e.ToProducto())
            .ToListAsync();
    }

    public async Task<Result<Producto, DomainError>> GetByIdAsync(int id)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        return Result.Success<Producto, DomainError>(entity.ToProducto());
    }

    public async Task<Result<Producto, DomainError>> CreateAsync(Producto producto)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            var entity = producto.ToEntity();
            context.Productos.Add(entity);
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al crear el producto {Nombre}", producto.Nombre);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    /// <summary>
    /// No copia SeguimientoIndividual a propósito: es un interruptor que solo activa la
    /// migración de datos (nunca el formulario de edición, que ni lo muestra), así que
    /// UpdateAsync no debe poder resetearlo sin querer al editar cualquier otro campo.
    /// </summary>
    public async Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto)
    {
        await InitializeAsync();
        try
        {
            // Concurrencia optimista: ver ReintentoConcurrencia y el token en AppDbContext.
            return await ReintentoConcurrencia.EjecutarAsync(async () =>
            {
                using var context = CreateContext();
                var entity = await context.Productos.FindAsync(id);
                if (entity is null)
                    return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));

                entity.Nombre = producto.Nombre;

                // El formulario de edición trae el stock que tenía al abrirse (StockActualAlLeer).
                // Si el usuario NO lo tocó, no se escribe: así editar el nombre o el precio no
                // pisa las ventas o recepciones hechas mientras el formulario estaba abierto.
                // Si lo cambió a propósito (corregir un recuento), sí se aplica. Sin
                // StockActualAlLeer (null) se conserva el comportamiento de siempre: se escribe.
                var stockSinTocar = producto.StockActualAlLeer is decimal leido && producto.StockActual == leido;
                if (!stockSinTocar)
                    entity.StockActual = producto.StockActual;

                entity.StockMaximo = producto.StockMaximo;
                entity.Unidad = producto.Unidad;
                entity.Descripcion = producto.Descripcion;
                entity.ImagenUrl = producto.ImagenUrl;
                entity.ProveedorId = producto.ProveedorId;
                entity.PrecioUnitario = producto.PrecioUnitario;
                entity.AplicaConciliacionTpv = producto.AplicaConciliacionTpv;
                entity.UnidadesPorPack = producto.UnidadesPorPack;
                entity.Pasillo = producto.Pasillo;
                entity.OrdenEnPasillo = producto.OrdenEnPasillo;
                entity.EsFrontalPasillo = producto.EsFrontalPasillo;
                entity.Armario = producto.Armario;
                entity.Seccion = producto.Seccion;
                entity.Pais = producto.Pais;
                entity.Region = producto.Region;
                entity.Finca = producto.Finca;
                entity.Altitud = producto.Altitud;
                entity.Variedad = producto.Variedad;
                entity.Proceso = producto.Proceso;
                entity.NotasCata = producto.NotasCata;
                await context.SaveChangesAsync();
                return Result.Success<Producto, DomainError>(entity.ToProducto());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al editar el producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Producto, DomainError>> DeleteAsync(int id)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            context.Productos.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar el producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }



    public async Task<Result<Producto, DomainError>> ConfirmarRecepcionAsync(int id, int cantidadRecibida)
    {
        await InitializeAsync();
        try
        {
            // Concurrencia optimista: ver ReintentoConcurrencia y el token en AppDbContext.
            return await ReintentoConcurrencia.EjecutarAsync(async () =>
            {
                using var context = CreateContext();
                var entity = await context.Productos.FindAsync(id);
                if (entity is null)
                    return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));

                entity.StockActual += cantidadRecibida;
                await context.SaveChangesAsync();
                return Result.Success<Producto, DomainError>(entity.ToProducto());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al confirmar la recepción del producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Producto, DomainError>> AjustarStockActualAsync(int id, int delta)
    {
        await InitializeAsync();
        try
        {
            // Concurrencia optimista: ver ReintentoConcurrencia y el token en AppDbContext.
            return await ReintentoConcurrencia.EjecutarAsync(async () =>
            {
                using var context = CreateContext();
                var entity = await context.Productos.FindAsync(id);
                if (entity is null)
                    return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));

                var nuevoStock = entity.StockActual + delta;
                if (nuevoStock < 0)
                    return Result.Failure<Producto, DomainError>(
                        ProductoErrors.Validation(["El stock no puede quedar por debajo de 0"]));

                entity.StockActual = nuevoStock;
                await context.SaveChangesAsync();
                return Result.Success<Producto, DomainError>(entity.ToProducto());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al ajustar el stock actual del producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<IEnumerable<Producto>> ProductosUrgentes()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Productos
            .Where(e => e.StockActual < e.StockMaximo)
            .Select(e => e.ToProducto())
            .ToListAsync();
    }

    public async Task<Result<Producto, DomainError>> ActualizarStockActualAsync(int id, decimal nuevaCantidad)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            entity.StockActual = Math.Round(nuevaCantidad, 2, MidpointRounding.AwayFromZero);
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al actualizar el stock actual del producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Producto, DomainError>> ActualizarPrecioUnitarioAsync(int id, decimal nuevoPrecio)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            entity.PrecioUnitario = nuevoPrecio;
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al actualizar el precio unitario del producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }
}
