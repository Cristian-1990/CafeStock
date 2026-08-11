using CafeStock.Back.Infrastructure;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Services.Compras;
using CafeStock.Back.Services.Facturas;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
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
    builder.Services.AddScoped<CafeStock.Blazor.Services.PdfService>();
    builder.Services.Configure<CafeStock.Blazor.Services.EmailSettings>(builder.Configuration.GetSection("Email"));
    builder.Services.AddScoped<CafeStock.Blazor.Services.EmailService>();

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
