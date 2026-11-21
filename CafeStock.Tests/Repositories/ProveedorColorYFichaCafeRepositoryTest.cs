using CafeStock.Back.Entity;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace CafeStock.Tests.Repositories;

/// <summary>
/// Persistencia de lo añadido en la migración AddColorProveedorYFichaCafe: el color del proveedor
/// y los campos de la ficha de café del producto. Incluye la regla de que editar un proveedor sin
/// informar el color (un formulario que no lo conoce) no lo borra.
/// </summary>
[TestFixture]
public class ProveedorColorYFichaCafeRepositoryTest
{
    private string _dbPath = string.Empty;
    private string _connectionString = string.Empty;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_color_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Test]
    public async Task Proveedor_GuardaYDevuelveElColor()
    {
        var repo = new ProveedoresEfRepository(_connectionString);

        var creado = await repo.CreateAsync(new Proveedor { Nombre = "Puchero", Color = "#3F6B2E" });
        var leido = await repo.GetByIdAsync(creado.Value.Id);

        leido.Value.Color.Should().Be("#3F6B2E");
    }

    [Test]
    public async Task Proveedor_UpdateConColor_LoCambia()
    {
        var repo = new ProveedoresEfRepository(_connectionString);
        var creado = await repo.CreateAsync(new Proveedor { Nombre = "Puchero", Color = "#3F6B2E" });

        var resultado = await repo.UpdateAsync(creado.Value.Id, creado.Value with { Color = "#1F5F8B" });

        resultado.Value.Color.Should().Be("#1F5F8B");
    }

    [Test]
    public async Task Proveedor_UpdateSinColor_ConservaElActual()
    {
        var repo = new ProveedoresEfRepository(_connectionString);
        var creado = await repo.CreateAsync(new Proveedor { Nombre = "Puchero", Color = "#3F6B2E" });

        var resultado = await repo.UpdateAsync(creado.Value.Id, creado.Value with { Nombre = "Puchero Café", Color = "" });

        resultado.Value.Nombre.Should().Be("Puchero Café");
        resultado.Value.Color.Should().Be("#3F6B2E");
    }

    [Test]
    public async Task Producto_GuardaYActualizaLaFichaDeCafe()
    {
        var repo = new ProductosEfRepository(_connectionString);
        var creado = await repo.CreateAsync(new Producto
        {
            Nombre = "Brasil Fazenda Boa Vista",
            StockMaximo = 4,
            SeguimientoIndividual = true,
            Pais = "Brasil",
            Region = "Alta Mogiana",
            Finca = "Fazenda Boa Vista",
            Altitud = "927 msnm",
            Variedad = "Catuaí amarillo",
            Proceso = "Natural",
            NotasCata = "Chocolate dulce y almendras",
        });

        var leido = (await repo.GetByIdAsync(creado.Value.Id)).Value;
        leido.Pais.Should().Be("Brasil");
        leido.Region.Should().Be("Alta Mogiana");
        leido.Finca.Should().Be("Fazenda Boa Vista");
        leido.Altitud.Should().Be("927 msnm");
        leido.Variedad.Should().Be("Catuaí amarillo");
        leido.Proceso.Should().Be("Natural");
        leido.NotasCata.Should().Be("Chocolate dulce y almendras");

        var actualizado = await repo.UpdateAsync(leido.Id, leido with { Proceso = "Lavado", NotasCata = "Cítrico" });
        actualizado.Value.Proceso.Should().Be("Lavado");
        actualizado.Value.NotasCata.Should().Be("Cítrico");
        actualizado.Value.Finca.Should().Be("Fazenda Boa Vista");
    }

    [Test]
    public async Task Migracion_DaUnColorDistintoALosProveedoresQueYaExisten()
    {
        // Arrange: base en el esquema ANTERIOR a esta migración, con 12 proveedores ya creados.
        await using (var contexto = new AppDbContext(_connectionString))
        {
            var migrator = contexto.GetService<IMigrator>();
            var anterior = contexto.Database.GetMigrations().Single(m => m.EndsWith("AddIdentity"));
            await migrator.MigrateAsync(anterior);
        }

        await using (var conexion = new SqliteConnection(_connectionString))
        {
            await conexion.OpenAsync();
            for (var i = 1; i <= 12; i++)
            {
                await using var comando = conexion.CreateCommand();
                comando.CommandText =
                    "INSERT INTO Proveedores (Nombre, ImagenUrl, Telefono, PersonaContacto, Email, DiaReparto, Notas, Direccion, NifCif, " +
                    $"EsSupermercadoGenerico, AgruparPorTipoUnidad, PrecioUnitarioAltaPrecision) VALUES ('P{i}', '', '', '', '', '', '', '', '', 0, 0, 0)";
                await comando.ExecuteNonQueryAsync();
            }
        }

        // Act: se aplica la migración pendiente
        await using (var contexto = new AppDbContext(_connectionString))
            await contexto.Database.MigrateAsync();

        // Assert
        var proveedores = (await new ProveedoresEfRepository(_connectionString).GetAllAsync()).OrderBy(p => p.Id).ToList();
        proveedores.Should().HaveCount(12);
        proveedores.Should().OnlyContain(p => PaletaProveedores.EsHexValido(p.Color));
        proveedores.Take(10).Select(p => p.Color).Should().OnlyHaveUniqueItems("los 10 primeros reciben los 10 tonos de la paleta");
        proveedores.Take(10).Select(p => p.Color).Should().BeEquivalentTo(PaletaProveedores.Colores);
        proveedores[10].Color.Should().Be(proveedores[0].Color, "con más de 10 proveedores la paleta se repite");
    }

    [Test]
    public async Task Migracion_Recolorea_Puchero_CocaCola_YAlcampo_ConSusColoresDeMarca()
    {
        await using (var contexto = new AppDbContext(_connectionString))
        {
            var anterior = contexto.Database.GetMigrations().Single(m => m.EndsWith("AddIdentity"));
            await contexto.GetService<IMigrator>().MigrateAsync(anterior);
        }

        await using (var conexion = new SqliteConnection(_connectionString))
        {
            await conexion.OpenAsync();
            foreach (var nombre in new[] { "Alcampo", "Puchero ", "Coca-Cola", "Mahou" })
            {
                await using var comando = conexion.CreateCommand();
                comando.CommandText =
                    "INSERT INTO Proveedores (Nombre, ImagenUrl, Telefono, PersonaContacto, Email, DiaReparto, Notas, Direccion, NifCif, " +
                    $"EsSupermercadoGenerico, AgruparPorTipoUnidad, PrecioUnitarioAltaPrecision) VALUES ('{nombre}', '', '', '', '', '', '', '', '', 0, 0, 0)";
                await comando.ExecuteNonQueryAsync();
            }
        }

        await using (var contexto = new AppDbContext(_connectionString))
            await contexto.Database.MigrateAsync();

        var porNombre = (await new ProveedoresEfRepository(_connectionString).GetAllAsync())
            .ToDictionary(p => p.Nombre.Trim(), p => p.Color);

        porNombre["Puchero"].Should().Be("#8B4513");     // marrón
        porNombre["Coca-Cola"].Should().Be("#F40009");   // rojo intenso
        porNombre["Alcampo"].Should().Be("#D9534F");     // rojo más suave
        PaletaProveedores.Colores.Should().Contain(porNombre["Mahou"], "el resto recibe un tono de la paleta viva");
        porNombre.Values.Should().OnlyHaveUniqueItems();
    }
}
