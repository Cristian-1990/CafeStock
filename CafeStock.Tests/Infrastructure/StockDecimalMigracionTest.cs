using CafeStock.Back.Entity;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Repositories.Productos.EfCore;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CafeStock.Tests.Infrastructure;

/// <summary>
/// Regresión del paso del stock de entero a decimal: los productos que ya existían con stock entero deben
/// poder actualizarse después de migrar. SQLite guarda los decimales como texto y la comprobación de
/// concurrencia del stock compara texto con texto, así que un "1" antiguo no coincidía con el "1.0" que
/// escribe EF y toda actualización fallaba (ver la migración NormalizarStockDecimal).
/// </summary>
[TestFixture]
public class StockDecimalMigracionTest
{
    // Última migración anterior al cambio a decimal.
    private const string MigracionAnterior = "20261125154040_AddStocksRealizados";

    private string _dbPath;
    private string _connectionString;

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

    [Test]
    public async Task ProductoConStockEnteroAntiguo_TrasMigrar_SePuedeActualizarConDecimales()
    {
        // Arrange: base de datos tal como estaba antes del cambio, con un producto de stock entero
        using (var contexto = new AppDbContext(_connectionString))
        {
            await contexto.GetService<IMigrator>().MigrateAsync(MigracionAnterior);
            await contexto.Database.ExecuteSqlRawAsync(
                "INSERT INTO Productos (Nombre, StockActual, StockMaximo, Unidad, Descripcion, ImagenUrl, PrecioUnitario, " +
                "SeguimientoIndividual, AplicaConciliacionTpv, OrdenEnPasillo, EsFrontalPasillo, Seccion, Pais, Region, " +
                "Finca, Altitud, Variedad, Proceso, NotasCata) " +
                "VALUES ('Harina', 2, 5, 0, '', '', 0, 0, 0, 0, 0, 0, '', '', '', '', '', '', '')");
        }
        SqliteConnection.ClearAllPools();

        // Act: arranque normal (aplica StockDecimal y NormalizarStockDecimal) y actualización a medio paquete
        await DatabaseInitializer.EnsureInitializedAsync(_connectionString);
        var repositorio = new ProductosEfRepository(_connectionString);
        var antiguo = (await repositorio.GetAllAsync()).Single();
        var actualizado = await repositorio.ActualizarStockActualAsync(antiguo.Id, 0.5m);

        // Assert
        antiguo.StockActual.Should().Be(2m);
        antiguo.StockMaximo.Should().Be(5m);
        actualizado.IsSuccess.Should().BeTrue(actualizado.IsFailure ? actualizado.Error.Message : "");
        actualizado.Value.StockActual.Should().Be(0.5m);
    }
}
