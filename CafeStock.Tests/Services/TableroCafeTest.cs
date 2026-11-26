using CafeStock.Back.Models;
using FluentAssertions;

namespace CafeStock.Tests.Services;

[TestFixture]
public class TableroCafeTest
{
    private static Producto Cafe(int id, string nombre, decimal actual, decimal maximo,
        GrupoCafe grupo = GrupoCafe.Casa, FormatoCafe formato = FormatoCafe.Cuarto) =>
        new() { Id = id, Nombre = nombre, StockActual = actual, StockMaximo = maximo, GrupoCafe = grupo, FormatoCafe = formato };

    private static RegistroCafe Uso(string quien, int minutos) =>
        new() { Id = minutos, NombreTrabajador = quien, Fecha = new DateTime(2026, 11, 26, 10, 0, 0).AddMinutes(minutos) };

    [Test]
    public void Huecos_TantosComoElMaximo_DisponiblesSonElStock()
    {
        var huecos = TableroCafe.Huecos(Cafe(1, "Brasil", actual: 4, maximo: 6), []);

        huecos.Should().HaveCount(6);
        huecos.Count(h => !h.Usado).Should().Be(4);
        huecos.Count(h => h.Usado).Should().Be(2);
    }

    [Test]
    public void Huecos_LosUsadosVanPrimeroYLosDisponiblesDespues()
    {
        var huecos = TableroCafe.Huecos(Cafe(1, "Brasil", 2, 5), []);

        huecos.Select(h => h.Usado).Should().Equal(true, true, true, false, false);
    }

    [Test]
    public void Huecos_LosUsadosLlevanElNombreDeQuienLosRegistroDeMasAntiguoAMasReciente()
    {
        // Los registros llegan con el más reciente primero: Ana fue la última.
        var registros = new List<RegistroCafe> { Uso("Ana", 30), Uso("Luis", 20), Uso("Marta", 10) };

        var huecos = TableroCafe.Huecos(Cafe(1, "Brasil", actual: 2, maximo: 5), registros);

        huecos.Where(h => h.Usado).Select(h => h.Trabajador).Should().Equal("Marta", "Luis", "Ana");
        huecos.Where(h => h.Usado).Select(h => h.Fecha!.Value.Minute).Should().Equal(10, 20, 30);
    }

    [Test]
    public void Huecos_LosUsadosGuardanElIdDeSuRegistro()
    {
        var registros = new List<RegistroCafe> { Uso("Ana", 30), Uso("Marta", 10) };

        var huecos = TableroCafe.Huecos(Cafe(1, "Brasil", actual: 1, maximo: 3), registros);

        huecos.Where(h => h.Usado).Select(h => h.RegistroId).Should().Equal(10, 30);
        huecos.Single(h => !h.Usado).RegistroId.Should().BeNull();
    }

    [Test]
    public void Huecos_ConMasRegistrosQueUsados_SoloMuestraLosMasRecientes()
    {
        var registros = new List<RegistroCafe> { Uso("Ana", 40), Uso("Luis", 30), Uso("Marta", 20), Uso("Pepe", 10) };

        var huecos = TableroCafe.Huecos(Cafe(1, "Brasil", actual: 4, maximo: 6), registros);   // 2 usados

        huecos.Where(h => h.Usado).Select(h => h.Trabajador).Should().Equal("Luis", "Ana");
    }

    [Test]
    public void Huecos_ConMenosRegistrosQueUsados_LosPrimerosUsadosSalenSinNombre()
    {
        var registros = new List<RegistroCafe> { Uso("Ana", 10) };

        var huecos = TableroCafe.Huecos(Cafe(1, "Brasil", actual: 1, maximo: 4), registros);   // 3 usados

        huecos.Where(h => h.Usado).Select(h => h.Trabajador).Should().Equal(null, null, "Ana");
    }

    [Test]
    public void Huecos_StockMayorQueElMaximo_NoEscondeStock()
    {
        var huecos = TableroCafe.Huecos(Cafe(1, "Brasil", actual: 8, maximo: 6), []);

        huecos.Should().HaveCount(8);
        huecos.Should().OnlyContain(h => !h.Usado);
    }

    [Test]
    public void Huecos_SinMaximoNiStock_NoHayHuecos()
    {
        TableroCafe.Huecos(Cafe(1, "Brasil", 0, 0), []).Should().BeEmpty();
    }
}
