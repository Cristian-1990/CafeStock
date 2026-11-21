using System.Collections.Concurrent;
using System.Reflection;
using CafeStock.Back.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Infrastructure;

/// <summary>
/// Deja la base de datos lista para usarse: crea el esquema o lo actualiza con EF Core
/// Migrations. Es idempotente y se ejecuta UNA sola vez por base de datos y proceso, aunque la
/// llamen a la vez los seis repositorios.
///
/// Hay tres casos posibles:
///  1. Base de datos nueva (no existe el fichero o no tiene tablas): se aplican todas las
///     migraciones, empezando por InitialCreate.
///  2. Base de datos HEREDADA: creada con el sistema antiguo (EnsureCreated + ALTER TABLE a
///     mano), por tanto con tablas pero sin tabla __EFMigrationsHistory. Se actualiza por
///     última vez con <see cref="AppDbContext.ActualizarEsquemaHeredadoAsync"/> y se registra
///     InitialCreate como YA aplicada (baseline), porque sus tablas ya existen y volver a
///     crearlas fallaría. A partir de ahí es un caso 3.
///  3. Base de datos ya gestionada por migraciones: solo se aplican las pendientes.
/// </summary>
public static class DatabaseInitializer
{
    private static readonly ConcurrentDictionary<string, Lazy<Task>> Inicializaciones = new();

    /// <summary>
    /// Inicializa la base de datos de <paramref name="connectionString"/> si aún no se hizo en
    /// este proceso. Si falla, el siguiente intento vuelve a intentarlo desde cero.
    /// </summary>
    public static async Task EnsureInitializedAsync(string connectionString)
    {
        var tarea = Inicializaciones.GetOrAdd(connectionString, cs => new Lazy<Task>(() => InicializarAsync(cs)));
        try
        {
            await tarea.Value;
        }
        catch
        {
            Inicializaciones.TryRemove(connectionString, out _);
            throw;
        }
    }

    private static async Task InicializarAsync(string connectionString)
    {
        await using var context = new AppDbContext(connectionString);

        var tieneTablas = await ExisteTablaAsync(context, "Productos");
        var tieneHistorial = await ExisteTablaAsync(context, "__EFMigrationsHistory");

        if (tieneTablas && !tieneHistorial)
        {
            Log.Information("Base de datos heredada detectada: se actualiza el esquema y se marca InitialCreate como aplicada");
            await context.ActualizarEsquemaHeredadoAsync();
            await MarcarBaselineAsync(context);
        }

        var pendientes = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (pendientes.Count > 0)
            Log.Information("Aplicando {Cantidad} migración(es): {Migraciones}", pendientes.Count, string.Join(", ", pendientes));

        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Crea __EFMigrationsHistory (con el formato exacto de EF Core) y registra la primera
    /// migración (InitialCreate) como aplicada sin ejecutarla.
    /// </summary>
    private static async Task MarcarBaselineAsync(AppDbContext context)
    {
        var migracionInicial = context.Database.GetMigrations().First();
        var versionEf = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0] ?? "10.0.0";

        var conexion = context.Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var crear = conexion.CreateCommand();
            crear.CommandText = """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                )
                """;
            await crear.ExecuteNonQueryAsync();

            await using var insertar = conexion.CreateCommand();
            insertar.CommandText = """
                INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ($id, $version)
                """;
            insertar.Parameters.Add(new SqliteParameter("$id", migracionInicial));
            insertar.Parameters.Add(new SqliteParameter("$version", versionEf));
            await insertar.ExecuteNonQueryAsync();
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    private static async Task<bool> ExisteTablaAsync(AppDbContext context, string tabla)
    {
        var conexion = context.Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $nombre";
            comando.Parameters.Add(new SqliteParameter("$nombre", tabla));
            return Convert.ToInt64(await comando.ExecuteScalarAsync()) > 0;
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }
}
