using CafeStock.Back.Auth;
using CafeStock.Back.Entity;
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
using CafeStock.Back.Repositories.HojasCafe.Base;
using CafeStock.Back.Repositories.HojasCafe.EfCore;
using CafeStock.Back.Repositories.StocksRealizados.Base;
using CafeStock.Back.Repositories.StocksRealizados.EfCore;
using CafeStock.Back.Services.Compras;
using CafeStock.Back.Services.Facturas;
using CafeStock.Back.Services.Informes;
using CafeStock.Back.Services.Presupuestos;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
using CafeStock.Back.Services.RegistrosCafe;
using CafeStock.Back.Services.SnapshotsInventario;
using CafeStock.Back.Services.HojasCafe;
using CafeStock.Back.Services.StocksRealizados;
using CafeStock.Back.Services.Usuarios;
using CafeStock.Back.Validators.Common;
using CafeStock.Back.Validators.Compras;
using CafeStock.Back.Validators.Presupuestos;
using CafeStock.Back.Validators.Productos;
using CafeStock.Back.Validators.Proveedores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddSingleton<IStockRealizadoRepository>(_ => new StocksRealizadosEfRepository(connectionString));
        services.AddSingleton<IHojaCafeRepository>(_ => new HojasCafeEfRepository(connectionString));

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
        services.AddScoped<IStockRealizadoService, StockRealizadoService>();
        services.AddScoped<IHojaCafeService, HojaCafeService>();

        // Cálculos de los informes: puros (sin base de datos); "hoy" sale del reloj inyectado.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IInformeService, InformeService>();

        // Singleton: abre un ámbito por operación (ver UsuarioService). Necesita además
        // AddCafeStockIdentity para poder resolver UserManager y RoleManager.
        services.AddSingleton<IUsuarioService, UsuarioService>();

        return services;
    }

    /// <summary>
    /// Registra ASP.NET Core Identity (usuarios y roles guardados en el mismo AppDbContext y la
    /// misma base de datos). Devuelve el IdentityBuilder para que la capa web encadene lo que
    /// necesita y el Back no (SignInManager, cookies), ya que depende de HttpContext.
    /// </summary>
    public static IdentityBuilder AddCafeStockIdentity(this IServiceCollection services, string connectionString)
    {
        // Identity resuelve el contexto desde el contenedor; el constructor recibe la cadena de conexión.
        services.AddScoped(_ => new AppDbContext(connectionString));

        return services
            .AddIdentityCore<IdentityUser>(opciones =>
            {
                // Política razonable para una cafetería: 8 caracteres con letras y un número.
                // Sin obligar a mayúsculas ni símbolos (se acaban apuntando en un post-it).
                opciones.Password.RequiredLength = 8;
                opciones.Password.RequireDigit = true;
                opciones.Password.RequireLowercase = true;
                opciones.Password.RequireUppercase = false;
                opciones.Password.RequireNonAlphanumeric = false;

                // Frena a quien pruebe contraseñas a ciegas: 5 fallos = 5 minutos bloqueado.
                opciones.Lockout.AllowedForNewUsers = true;
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                opciones.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddErrorDescriber<DescriptorErroresIdentityEs>()
            .AddEntityFrameworkStores<AppDbContext>();
    }
}
