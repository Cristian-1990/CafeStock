using CafeStock.Back.Models;
using CafeStock.Back.Repositories.StocksRealizados.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class StocksRealizadosEfRepositoryTest
{
    private string _dbPath;
    private StocksRealizadosEfRepository _repository;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _repository = new StocksRealizadosEfRepository($"Data Source={_dbPath}");
    }

    [TearDown]
    public void TearDown()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Test]
    public async Task GetAllAsync_SinMarcas_DevuelveVacio()
    {
        (await _repository.GetAllAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task MarcarAsync_GuardaLaMarcaDelProveedor()
    {
        var resultado = await _repository.MarcarAsync(7, new DateTime(2026, 11, 25, 10, 0, 0));

        resultado.IsSuccess.Should().BeTrue();
        var todas = (await _repository.GetAllAsync()).ToList();
        todas.Should().ContainSingle(m => m.ProveedorId == 7);
    }

    [Test]
    public async Task MarcarAsync_DosVecesElMismoProveedor_NoDuplicaYActualizaLaFecha()
    {
        await _repository.MarcarAsync(7, new DateTime(2026, 11, 25, 10, 0, 0));
        var segunda = await _repository.MarcarAsync(7, new DateTime(2026, 11, 26, 9, 0, 0));

        segunda.IsSuccess.Should().BeTrue();
        var todas = (await _repository.GetAllAsync()).ToList();
        todas.Should().ContainSingle();
        todas[0].Fecha.Should().Be(new DateTime(2026, 11, 26, 9, 0, 0));
    }

    [Test]
    public async Task MarcarAsync_SinProveedor_UsaElValorReservado()
    {
        await _repository.MarcarAsync(StockRealizado.SinProveedor, DateTime.Now);

        (await _repository.GetAllAsync()).Should().ContainSingle(m => m.ProveedorId == StockRealizado.SinProveedor);
    }

    [Test]
    public async Task QuitarAsync_QuitaSoloLaMarcaIndicada()
    {
        await _repository.MarcarAsync(1, DateTime.Now);
        await _repository.MarcarAsync(2, DateTime.Now);

        var resultado = await _repository.QuitarAsync(1);

        resultado.IsSuccess.Should().BeTrue();
        (await _repository.GetAllAsync()).Select(m => m.ProveedorId).Should().BeEquivalentTo(new[] { 2 });
    }

    [Test]
    public async Task QuitarAsync_SinMarca_NoDaError()
    {
        (await _repository.QuitarAsync(99)).IsSuccess.Should().BeTrue();
    }
}
