using CafeStock.Back.Repositories.StocksRealizados.EfCore;
using CafeStock.Back.Services.StocksRealizados;
using FluentAssertions;

namespace CafeStock.Tests.Services;

[TestFixture]
public class StockRealizadoServiceTest
{
    private string _dbPath;
    private StockRealizadoService _service;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _service = new StockRealizadoService(new StocksRealizadosEfRepository($"Data Source={_dbPath}"));
    }

    [TearDown]
    public void TearDown()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Test]
    public async Task GetProveedoresPendientesAsync_RepresentaSinProveedorConNull()
    {
        await _service.MarcarAsync(5);
        await _service.MarcarAsync(null);

        var pendientes = await _service.GetProveedoresPendientesAsync();

        pendientes.Should().BeEquivalentTo(new int?[] { 5, null });
    }

    [Test]
    public async Task QuitarAsync_TrasRecibir_YaNoEstaPendiente()
    {
        await _service.MarcarAsync(5);

        await _service.QuitarAsync(5);

        (await _service.GetProveedoresPendientesAsync()).Should().NotContain(5);
    }

    [Test]
    public async Task QuitarAsync_SinProveedor_QuitaLaMarcaNula()
    {
        await _service.MarcarAsync(null);
        await _service.MarcarAsync(8);

        await _service.QuitarAsync(null);

        (await _service.GetProveedoresPendientesAsync()).Should().BeEquivalentTo(new int?[] { 8 });
    }
}
