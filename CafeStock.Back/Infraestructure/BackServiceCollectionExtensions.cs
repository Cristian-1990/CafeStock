using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Repositories.Compras.EfCore;
using CafeStock.Back.Repositories.Presupuestos.Base;
using CafeStock.Back.Repositories.Presupuestos.EfCore;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using CafeStock.Back.Repositories.RegistrosCafe.Base;
using CafeStock.Back.Repositories.RegistrosCafe.EfCore;
using CafeStock.Back.Repositories.SnapshotsInventario.Base;
using CafeStock.Back.Repositories.SnapshotsInventario.EfCore;
using CafeStock.Back.Services.Compras;
using CafeStock.Back.Services.Facturas;
using CafeStock.Back.Services.Presupuestos;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
using CafeStock.Back.Services.RegistrosCafe;
using CafeStock.Back.Services.SnapshotsInventario;
using CafeStock.Back.Validators.Common;
using CafeStock.Back.Validators.Compras;
using CafeStock.Back.Validators.Presupuestos;
using CafeStock.Back.Validators.Productos;
using CafeStock.Back.Validators.Proveedores;
using Microsoft.Extensions.DependencyInjection;

namespace CafeStock.Back.Infrastructure;

/// <summary>
/// Único sitio donde se registran las dependencias del Back (repositorios, validadores y
/// servicios), directamente en el contenedor del host. Sustituye a DependenciesProvider, que
/// construía un segundo ServiceProvider aparte y obligaba a Program.cs a copiar cada servicio
/// a mano: dos listas que debían mantenerse sincronizadas.
/// </summary>
public static class BackServiceCollectionExtensions
{
    public static IServiceCollection AddCafeStockBack(this IServiceCollection services, string connectionString)
    {
        // Repositorios: singleton, sin estado. Cada operación crea y libera su propio
        // AppDbContext (equivale a una fábrica de contextos), así que son seguros entre hilos.
        services.AddSingleton<IProductoRepository>(_ => new ProductosEfRepository(connectionString));
        services.AddSingleton<IProveedorRepository>(_ => new ProveedoresEfRepository(connectionString));
        services.AddSingleton<ICompraRepository>(_ => new ComprasEfRepository(connectionString));
        services.AddSingleton<IRegistroCafeRepository>(_ => new RegistrosCafeEfRepository(connectionString));
        services.AddSingleton<IPresupuestoRepository>(_ => new PresupuestosEfRepository(connectionString));
        services.AddSingleton<ISnapshotInventarioRepository>(_ => new SnapshotsInventarioEfRepository(connectionString));

        services.AddScoped<IValidador<Producto>, ValidadorProducto>();
        services.AddScoped<IValidador<Proveedor>, ValidadorProveedor>();
        services.AddScoped<IValidador<Compra>, ValidadorCompra>();
        services.AddScoped<IValidador<Presupuesto>, ValidadorPresupuesto>();

        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        // CompraService depende de IProductoService (sincroniza Producto.PrecioUnitario tras
        // registrar una compra); el contenedor lo resuelve solo, el orden aquí no importa.
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IFacturaService, FacturaService>();
        services.AddScoped<IRegistroCafeService, RegistroCafeService>();
        services.AddScoped<IPresupuestoService, PresupuestoService>();
        services.AddScoped<ISnapshotInventarioService, SnapshotInventarioService>();

        return services;
    }
}
