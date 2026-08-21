using CafeStock.Back.Infrastructure;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Repositories.Presupuestos.Base;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Repositories.RegistrosCafe.Base;
using CafeStock.Back.Repositories.SnapshotsInventario.Base;
using CafeStock.Back.Services.Compras;
using CafeStock.Back.Services.Facturas;
using CafeStock.Back.Services.Presupuestos;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
using CafeStock.Back.Services.RegistrosCafe;
using CafeStock.Back.Services.SnapshotsInventario;
using ApexCharts;
using CafeStock.Blazor.Components;
using MudBlazor.Services;
using Serilog;

// Logger de arranque: captura también los errores que puedan ocurrir antes de
// que WebApplication.CreateBuilder termine de construir el host.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Arrancando CafeStock");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog sustituye al logging por defecto; captura también los mensajes
    // de arranque/parada que ya emite ASP.NET Core (Microsoft.Hosting.Lifetime).
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddMudServices();
    // Paleta café de la app como colores por defecto de todas las gráficas
    builder.Services.AddApexCharts(e =>
    {
        e.GlobalOptions = new ApexChartBaseOptions
        {
            Colors = new List<string> { "#6D4C41", "#C17817" }
        };
    });
    builder.Services.AddScoped<CafeStock.Blazor.Services.PdfService>();
    builder.Services.Configure<CafeStock.Blazor.Services.EmailSettings>(builder.Configuration.GetSection("Email"));
    builder.Services.AddScoped<CafeStock.Blazor.Services.EmailService>();
    // Scoped, no Singleton: una instancia por circuito (por tablet/pestaña), no compartida
    // entre todos los usuarios conectados a la vez — ver comentario en MascotSettingsService.
    builder.Services.AddScoped<CafeStock.Blazor.Services.MascotSettingsService>();

    // Configurar la base de datos
    var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cafestock.db");
    var connectionString = $"Data Source={dbPath}";

    // Registrar dependencias del Back
    var provider = DependenciesProvider.BuildServiceProvider(connectionString);
    builder.Services.AddSingleton(provider.GetService<IProductoRepository>()!);
    builder.Services.AddScoped<IProductoService, CafeStock.Back.Services.Productos.ProductoService>();
    builder.Services.AddScoped<CafeStock.Back.Validators.Common.IValidador<CafeStock.Back.Models.Producto>, CafeStock.Back.Validators.Productos.ValidadorProducto>();
    builder.Services.AddSingleton(provider.GetService<IProveedorRepository>()!);
    builder.Services.AddScoped<IProveedorService, CafeStock.Back.Services.Proveedores.ProveedorService>();
    builder.Services.AddScoped<CafeStock.Back.Validators.Common.IValidador<CafeStock.Back.Models.Proveedor>, CafeStock.Back.Validators.Proveedores.ValidadorProveedor>();
    builder.Services.AddSingleton(provider.GetService<ICompraRepository>()!);
    builder.Services.AddScoped<ICompraService, CafeStock.Back.Services.Compras.CompraService>();
    builder.Services.AddScoped<CafeStock.Back.Validators.Common.IValidador<CafeStock.Back.Models.Compra>, CafeStock.Back.Validators.Compras.ValidadorCompra>();
    builder.Services.AddScoped<IFacturaService, CafeStock.Back.Services.Facturas.FacturaService>();
    builder.Services.AddSingleton(provider.GetService<IRegistroCafeRepository>()!);
    builder.Services.AddScoped<IRegistroCafeService, CafeStock.Back.Services.RegistrosCafe.RegistroCafeService>();
    builder.Services.AddSingleton(provider.GetService<IPresupuestoRepository>()!);
    builder.Services.AddScoped<IPresupuestoService, CafeStock.Back.Services.Presupuestos.PresupuestoService>();
    builder.Services.AddScoped<CafeStock.Back.Validators.Common.IValidador<CafeStock.Back.Models.Presupuesto>, CafeStock.Back.Validators.Presupuestos.ValidadorPresupuesto>();
    builder.Services.AddSingleton(provider.GetService<ISnapshotInventarioRepository>()!);
    builder.Services.AddScoped<ISnapshotInventarioService, CafeStock.Back.Services.SnapshotsInventario.SnapshotInventarioService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseAntiforgery();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CafeStock ha terminado de forma inesperada durante el arranque");
}
finally
{
    Log.Information("Cerrando CafeStock");
    Log.CloseAndFlush();
}
