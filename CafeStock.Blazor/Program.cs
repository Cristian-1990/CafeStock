using CafeStock.Back.Auth;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Services.Usuarios;
using CafeStock.Blazor.Endpoints;
using CafeStock.Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
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
using Microsoft.Extensions.FileProviders;
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
    builder.Services.AddScoped<CafeStock.Blazor.Services.TemaSettingsService>();
    // Singleton: diccionario estático de solo lectura, sin estado por circuito.
    builder.Services.AddSingleton<CafeStock.Blazor.Services.IMascotMessageProvider, CafeStock.Blazor.Services.MascotMessageProvider>();

    // Configurar la base de datos — en Development (dotnet run, ver launchSettings.json) usa un
    // fichero de pruebas aparte, NUNCA la base de datos real de producción (cafestock.db, la
    // que usa el servicio systemd en Production). Así se puede levantar la app en local/en la
    // Pi para probar cambios sin arriesgar el stock/compras real de la cafetería.
    var dbFileName = builder.Environment.IsDevelopment() ? "cafestock-test.db" : "cafestock.db";
    var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dbFileName);
    var connectionString = $"Data Source={dbPath}";

    // Crea o actualiza el esquema con EF Core Migrations ANTES de aceptar peticiones: si la base
    // de datos no se puede preparar, la app falla al arrancar con un error claro en el log en
    // vez de dar un HTTP 500 en la primera página que se visite. Las bases antiguas (creadas
    // sin migraciones) se actualizan y se marcan como baseline automáticamente.
    await DatabaseInitializer.EnsureInitializedAsync(connectionString);

    // Registrar dependencias del Back (repositorios, validadores y servicios) en el contenedor del host
    builder.Services.AddCafeStockBack(connectionString);

    // Autenticación: usuarios y roles con ASP.NET Core Identity (guardados en la misma base de
    // datos) y sesión por cookie. SignInManager vive aquí y no en el Back porque necesita HttpContext.
    builder.Services.AddCafeStockIdentity(connectionString).AddSignInManager();
    builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
        .AddIdentityCookies(cookies => cookies.ApplicationCookie?.Configure(opciones =>
        {
            opciones.Cookie.Name = "CafeStock.Sesion";
            opciones.LoginPath = "/login";
            opciones.AccessDeniedPath = "/acceso-denegado";
            // Tablet de la cafetería: la sesión aguanta un turno largo y se renueva con el uso.
            opciones.ExpireTimeSpan = TimeSpan.FromHours(12);
            opciones.SlidingExpiration = true;
        }));

    // Cada política incluye los roles superiores (ver Politicas.RolesPermitidos).
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(Politicas.Trabajador, p => p.RequireRole(Politicas.RolesPermitidos(Politicas.Trabajador)))
        .AddPolicy(Politicas.Encargado, p => p.RequireRole(Politicas.RolesPermitidos(Politicas.Encargado)))
        .AddPolicy(Politicas.Admin, p => p.RequireRole(Politicas.RolesPermitidos(Politicas.Admin)));
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthenticationStateProvider, RevalidacionSesion>();

    var app = builder.Build();

    // Los tres roles tienen que existir antes de que nadie pueda iniciar sesión.
    await app.Services.GetRequiredService<IUsuarioService>().AsegurarRolesAsync();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    // Orden obligatorio: primero saber quién es (autenticación), luego qué puede hacer
    // (autorización) y después validar el token antifalsificación de los formularios.
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    // Complementa a MapStaticAssets: ese middleware solo conoce el manifiesto
    // generado en el último dotnet publish, así que imágenes de proveedores/
    // productos añadidas a mano después (ej. scp a producción) darían 404
    // hasta el siguiente publish. Este middleware sirve wwwroot/images/
    // directamente desde disco en cada petición, sin depender del manifiesto.
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "images")),
        RequestPath = "/images"
    });
    app.MapStaticAssets();
    app.MapAuthEndpoints();
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
