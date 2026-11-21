using CafeStock.Back.Entity;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CafeStock.Tests.Infrastructure;

/// <summary>
/// Cubre los tres casos de DatabaseInitializer: base nueva, base heredada (creada con el
/// sistema antiguo EnsureCreated + ALTER TABLE, sin tabla __EFMigrationsHistory) y base ya
/// gestionada por migraciones. Una "base heredada" se simula creándola con EnsureCreated, que
/// es exactamente lo que hacía la app antes: esquema completo pero sin historial de migraciones.
/// </summary>
[TestFixture]
public class DatabaseInitializerTest
{
    private string _dbPath = null!;
    private string _connectionString = null!;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    // ---------- Base de datos nueva ----------

    [Test]
    public async Task BaseNueva_AplicaLasMigracionesYCreaTodasLasTablas()
    {
        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert
        var tablas = await ListarTablasAsync();
        tablas.Should().Contain(["Productos", "Proveedores", "Compras", "LineasCompra",
            "RegistrosCafe", "Presupuestos", "SnapshotsInventario", "__EFMigrationsHistory"]);

        await using var context = new AppDbContext(_connectionString);
        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await context.Database.GetAppliedMigrationsAsync()).Should().BeEquivalentTo(context.Database.GetMigrations());
    }

    [Test]
    public async Task BaseNueva_NoAplicaLasCorreccionesDeDatosHeredadas()
    {
        // Arrange: primero la base nueva, luego un proveedor "Puchero" creado por el usuario
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);
        var proveedores = new ProveedoresEfRepository(_connectionString);
        await proveedores.CreateAsync(new Proveedor { Nombre = "Puchero" });

        // Act: otro "arranque" sobre la misma base (el caché del proceso no cuenta como arranque)
        var todos = (await new ProveedoresEfRepository(_connectionString).GetAllAsync()).ToList();

        // Assert: las correcciones de datos eran de un solo uso para la base antigua, no
        // deben reactivarse en bases gestionadas por migraciones (el usuario puede desactivarlas).
        todos.Single().AgruparPorTipoUnidad.Should().BeFalse();
    }

    [Test]
    public async Task EnsureInitializedAsync_LlamadoAlaVezPorMuchosHilos_NoFalla()
    {
        // Arrange: los seis repositorios lo llaman a la vez en el primer arranque real
        var salida = new TaskCompletionSource();
        var llamadas = Enumerable.Range(0, 12).Select(async _ =>
        {
            await salida.Task;
            await DatabaseInitializer.EnsureInitializedAsync(_connectionString);
        }).ToList();

        // Act
        salida.SetResult();
        var accion = async () => await Task.WhenAll(llamadas);

        // Assert
        await accion.Should().NotThrowAsync();
        (await ListarTablasAsync()).Should().Contain("Productos");
    }

    [Test]
    public async Task Migracion_InitialCreate_ProduceElMismoEsquemaQueElModelo()
    {
        // Arrange: una base creada por migraciones y otra creada directamente desde el modelo
        // (EnsureCreated) deben tener las mismas tablas y columnas con los mismos tipos. Si
        // alguien cambia el modelo y olvida generar la migración, este test lo detecta.
        var rutaModelo = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        try
        {
            await using (var contextoModelo = new AppDbContext($"Data Source={rutaModelo}"))
                await contextoModelo.Database.EnsureCreatedAsync();

            // Act
            await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

            // Assert
            var esquemaMigraciones = await LeerEsquemaAsync(_connectionString);
            var esquemaModelo = await LeerEsquemaAsync($"Data Source={rutaModelo}");
            esquemaMigraciones.Should().BeEquivalentTo(esquemaModelo);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(rutaModelo)) File.Delete(rutaModelo);
        }
    }

    // ---------- Base de datos heredada ----------

    [Test]
    public async Task BaseHeredada_SinHistorial_ConservaLosDatosYMarcaLaMigracionInicialComoAplicada()
    {
        // Arrange
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProveedorAsync(context, "Alcampo");
            await SembrarProductoAsync(context, "Harina", stockActual: 3, stockMaximo: 9, unidad: UnidadMedida.SinEspecificar, proveedorId: 1);
        });
        (await ListarTablasAsync()).Should().NotContain("__EFMigrationsHistory");

        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert: datos intactos y migración inicial registrada sin haberse ejecutado
        var productos = (await new ProductosEfRepository(_connectionString).GetAllAsync()).ToList();
        productos.Should().ContainSingle().Which.Should().Match<Producto>(p => p.Nombre == "Harina" && p.StockActual == 3);

        await using var context = new AppDbContext(_connectionString);
        (await context.Database.GetAppliedMigrationsAsync()).Should().BeEquivalentTo(context.Database.GetMigrations());
        (await ListarTablasAsync()).Should().Contain(["AspNetUsers", "AspNetRoles"]); // migraciones posteriores al baseline sí se aplican
        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task BaseHeredada_SinColumnasRecientes_LasAnadeSinPerderDatos()
    {
        // Arrange: base de antes de que existieran varias columnas, con un proveedor ya creado
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProveedorAsync(context, "Alcampo");
            await EjecutarAsync(context, "ALTER TABLE Proveedores DROP COLUMN PrecioUnitarioAltaPrecision");
            await EjecutarAsync(context, "ALTER TABLE Productos DROP COLUMN Seccion");
            await EjecutarAsync(context, "ALTER TABLE Compras DROP COLUMN Notas");
        });

        // Act
        var accion = async () => await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert: regresión del HTTP 500 real — una consulta LINQ revienta si falta cualquier columna
        await accion.Should().NotThrowAsync();
        var proveedores = (await new ProveedoresEfRepository(_connectionString).GetAllAsync()).ToList();
        proveedores.Should().ContainSingle().Which.Nombre.Should().Be("Alcampo");
        (await new ProductosEfRepository(_connectionString).GetAllAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task BaseHeredada_ConProveedorPuchero_ActivaAgruparPorTipoUnidad()
    {
        // Arrange
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProveedorAsync(context, "Puchero");
        });

        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert
        var proveedores = (await new ProveedoresEfRepository(_connectionString).GetAllAsync()).ToList();
        proveedores.Single(p => p.Nombre == "Puchero").AgruparPorTipoUnidad.Should().BeTrue();
    }

    [Test]
    public async Task BaseHeredada_SinProveedorPuchero_NoActivaAgruparPorTipoUnidad()
    {
        // Arrange
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProveedorAsync(context, "Alcampo");
        });

        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert
        (await new ProveedoresEfRepository(_connectionString).GetAllAsync()).Single()
            .AgruparPorTipoUnidad.Should().BeFalse();
    }

    [TestCase("Priégola")]
    [TestCase("Priégola ")] // regresión de un bug real: el nombre en producción lleva espacio final
    public async Task BaseHeredada_ConProveedorPriegola_ActivaPrecioUnitarioAltaPrecision(string nombre)
    {
        // Arrange
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProveedorAsync(context, nombre);
        });

        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert
        (await new ProveedoresEfRepository(_connectionString).GetAllAsync()).Single()
            .PrecioUnitarioAltaPrecision.Should().BeTrue();
    }

    [Test]
    public async Task BaseHeredada_SinProveedorPriegola_NoActivaPrecioUnitarioAltaPrecision()
    {
        // Arrange
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProveedorAsync(context, "Alcampo");
        });

        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert
        (await new ProveedoresEfRepository(_connectionString).GetAllAsync()).Single()
            .PrecioUnitarioAltaPrecision.Should().BeFalse();
    }

    [Test]
    public async Task BaseHeredada_ProveedorPucheroConDosBrasil_ActivaSeguimientoIndividualYRenombra()
    {
        // Arrange: los dos cafés de Puchero tal y como estaban guardados (mismo nombre, distinta unidad)
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProveedorAsync(context, "Puchero"); // primer proveedor: Id = 1
            await SembrarProductoAsync(context, "Brasil", stockActual: 12, stockMaximo: 12, unidad: UnidadMedida.Kg, proveedorId: 1);
            await SembrarProductoAsync(context, "Brasil", stockActual: 4, stockMaximo: 4, unidad: UnidadMedida.Unidades, proveedorId: 1);
        });

        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert
        var productos = (await new ProductosEfRepository(_connectionString).GetAllAsync()).ToList();
        var grano = productos.Single(p => p.Unidad == UnidadMedida.Kg);
        grano.Nombre.Should().Be("Brasil — Grano 1kg");
        grano.SeguimientoIndividual.Should().BeTrue();
        var cuarto = productos.Single(p => p.Unidad == UnidadMedida.Unidades);
        cuarto.Nombre.Should().Be("Brasil — Cuarto 250g");
        cuarto.SeguimientoIndividual.Should().BeTrue();
    }

    [Test]
    public async Task BaseHeredada_ProductoBrasilDeOtroProveedor_NoSeRenombra()
    {
        // Arrange: un producto que por casualidad se llama "Brasil" pero no es de Puchero
        await CrearBaseHeredadaAsync(async context =>
        {
            await SembrarProductoAsync(context, "Brasil", stockActual: 1, stockMaximo: 1, unidad: UnidadMedida.SinEspecificar, proveedorId: null);
        });

        // Act
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);

        // Assert
        var producto = (await new ProductosEfRepository(_connectionString).GetAllAsync()).Single();
        producto.Nombre.Should().Be("Brasil");
        producto.SeguimientoIndividual.Should().BeFalse();
    }

    // ---------- Ayudas ----------

    /// <summary>
    /// Crea una base como la que tenía la app antes de las migraciones: el esquema de InitialCreate
    /// (el del sistema antiguo, sin tablas de Identity) pero SIN tabla de historial. No sirve
    /// EnsureCreated: crearía el modelo actual completo, con tablas posteriores a InitialCreate.
    /// </summary>
    private async Task CrearBaseHeredadaAsync(Func<AppDbContext, Task> sembrar)
    {
        await using var context = new AppDbContext(_connectionString);
        var migracionInicial = context.Database.GetMigrations().First();
        await context.GetService<IMigrator>().MigrateAsync(migracionInicial);
        await EjecutarAsync(context, "DROP TABLE IF EXISTS __EFMigrationsHistory");
        await EjecutarAsync(context, "DROP TABLE IF EXISTS __EFMigrationsLock");
        await sembrar(context);
    }

    // Siembra con SQL crudo y SOLO con las columnas del esquema de InitialCreate: la base heredada
    // simula el esquema antiguo, y una inserción con las entidades actuales fallaría en cuanto una
    // migración posterior añada columnas ("table X has no column named Y").
    private static Task SembrarProveedorAsync(AppDbContext context, string nombre) =>
        EjecutarAsync(context,
            "INSERT INTO Proveedores (Nombre, ImagenUrl, Telefono, PersonaContacto, Email, DiaReparto, Notas, Direccion, NifCif, " +
            "EsSupermercadoGenerico, AgruparPorTipoUnidad, PrecioUnitarioAltaPrecision) " +
            $"VALUES ('{nombre.Replace("'", "''")}', '', '', '', '', '', '', '', '', 0, 0, 0)");

    private static Task SembrarProductoAsync(
        AppDbContext context, string nombre, int stockActual, int stockMaximo, UnidadMedida unidad, int? proveedorId) =>
        EjecutarAsync(context,
            "INSERT INTO Productos (Nombre, StockActual, StockMaximo, Unidad, Descripcion, ImagenUrl, ProveedorId, PrecioUnitario, " +
            "SeguimientoIndividual, AplicaConciliacionTpv, UnidadesPorPack, Pasillo, OrdenEnPasillo, EsFrontalPasillo, Seccion) " +
            $"VALUES ('{nombre.Replace("'", "''")}', {stockActual}, {stockMaximo}, {(int)unidad}, '', '', " +
            $"{(proveedorId is null ? "NULL" : proveedorId.ToString())}, '0', 0, 0, NULL, NULL, 0, 0, 0)");

    private static async Task EjecutarAsync(AppDbContext context, string sql)
    {
        var conexion = context.Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = sql;
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    private async Task<List<string>> ListarTablasAsync()
    {
        await using var conexion = new SqliteConnection(_connectionString);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
        var tablas = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            tablas.Add(lector.GetString(0));
        return tablas;
    }

    /// <summary>Esquema como lista "Tabla.Columna:tipo:notnull:pk", sin el historial de migraciones.</summary>
    private static async Task<List<string>> LeerEsquemaAsync(string connectionString)
    {
        await using var conexion = new SqliteConnection(connectionString);
        await conexion.OpenAsync();

        var tablas = new List<string>();
        await using (var comando = conexion.CreateCommand())
        {
            comando.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%'";
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                tablas.Add(lector.GetString(0));
        }

        var columnas = new List<string>();
        foreach (var tabla in tablas)
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = $"PRAGMA table_info(\"{tabla}\")";
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                columnas.Add($"{tabla}.{lector.GetString(1)}:{lector.GetString(2)}:{lector.GetInt32(3)}:{lector.GetInt32(5)}");
        }
        return columnas.OrderBy(c => c).ToList();
    }
}
