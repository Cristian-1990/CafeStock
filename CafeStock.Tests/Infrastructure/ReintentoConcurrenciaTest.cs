using CafeStock.Back.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CafeStock.Tests.Infrastructure;

[TestFixture]
public class ReintentoConcurrenciaTest
{
    private static SqliteException Sqlite(int codigo) => new("simulado", codigo);

    [Test]
    public void EsReintentable_ConflictoDeConcurrenciaDeEf_EsTrue()
    {
        ReintentoConcurrencia.EsReintentable(new DbUpdateConcurrencyException("x")).Should().BeTrue();
    }

    [TestCase(5)]   // SQLITE_BUSY: "database is locked"
    [TestCase(6)]   // SQLITE_LOCKED
    public void EsReintentable_BloqueoDeSqlite_EsTrueTantoSueltoComoDentroDeDbUpdateException(int codigo)
    {
        ReintentoConcurrencia.EsReintentable(Sqlite(codigo)).Should().BeTrue();
        ReintentoConcurrencia.EsReintentable(new DbUpdateException("guardando", Sqlite(codigo))).Should().BeTrue();
    }

    [Test]
    public void EsReintentable_OtrosErrores_EsFalse()
    {
        ReintentoConcurrencia.EsReintentable(Sqlite(19)).Should().BeFalse(); // SQLITE_CONSTRAINT: reintentar no lo arregla
        ReintentoConcurrencia.EsReintentable(new DbUpdateException("x", Sqlite(19))).Should().BeFalse();
        ReintentoConcurrencia.EsReintentable(new DbUpdateException("x")).Should().BeFalse();
        ReintentoConcurrencia.EsReintentable(new InvalidOperationException()).Should().BeFalse();
    }

    [Test]
    public async Task EjecutarAsync_FallaPorBloqueoVariasVecesYLuegoFunciona_DevuelveElResultado()
    {
        var llamadas = 0;

        var resultado = await ReintentoConcurrencia.EjecutarAsync(() =>
        {
            llamadas++;
            if (llamadas <= 3) throw Sqlite(5);
            return Task.FromResult("ok");
        });

        resultado.Should().Be("ok");
        llamadas.Should().Be(4);
    }

    [Test]
    public async Task EjecutarAsync_ErrorNoReintentable_SePropagaALaPrimera()
    {
        var llamadas = 0;

        var accion = async () => await ReintentoConcurrencia.EjecutarAsync<int>(() =>
        {
            llamadas++;
            throw new InvalidOperationException("no se arregla reintentando");
        });

        await accion.Should().ThrowAsync<InvalidOperationException>();
        llamadas.Should().Be(1);
    }

    [Test]
    public async Task EjecutarAsync_SiempreBloqueado_SeRindeTrasElMaximoDeIntentos()
    {
        var llamadas = 0;

        var accion = async () => await ReintentoConcurrencia.EjecutarAsync<int>(() =>
        {
            llamadas++;
            throw Sqlite(5);
        });

        await accion.Should().ThrowAsync<SqliteException>();
        llamadas.Should().Be(ReintentoConcurrencia.MaxIntentos);
    }
}
