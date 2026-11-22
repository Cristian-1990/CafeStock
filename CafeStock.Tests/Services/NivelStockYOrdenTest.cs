using CafeStock.Back.Models;
using FluentAssertions;

namespace CafeStock.Tests.Services;

[TestFixture]
public class NivelStockYOrdenTest
{
    // ---------- NivelStock ----------

    [TestCase(0, 12, 0)]
    [TestCase(6, 12, 50)]
    [TestCase(12, 12, 100)]
    [TestCase(20, 12, 100)]   // por encima del máximo: se recorta a 100
    [TestCase(3, 0, 0)]       // sin máximo definido
    [TestCase(-2, 10, 0)]     // nunca negativo
    public void Porcentaje_SeAcotaEntre0Y100(int actual, int maximo, int esperado)
    {
        NivelStock.Porcentaje(actual, maximo).Should().Be(esperado);
    }

    [TestCase(100, 5)]
    [TestCase(50, 5)]   // el 50 % ya es verde
    [TestCase(49, 4)]
    [TestCase(38, 4)]
    [TestCase(37, 3)]
    [TestCase(25, 3)]
    [TestCase(24, 2)]
    [TestCase(13, 2)]
    [TestCase(12, 1)]
    [TestCase(1, 1)]
    [TestCase(0, 1)]    // el último tramo es el rojo, también con stock a cero
    public void Tramo_SegunElPorcentaje(int porcentaje, int esperado)
    {
        NivelStock.Tramo(porcentaje).Should().Be(esperado);
    }

    [Test]
    public void Clase_UsaElPorcentajeSobreElMaximo()
    {
        NivelStock.Clase(6, 12).Should().Be("n5");
        NivelStock.Clase(0, 12).Should().Be("n1");
        NivelStock.Clase(4, 12).Should().Be("n3");   // 33 %
    }

    // ---------- Urgencia ----------

    private static Producto P(string nombre, int actual, int maximo) =>
        new() { Nombre = nombre, StockActual = actual, StockMaximo = maximo };

    [Test]
    public void PorUrgencia_OrdenaPorMenorPorcentajeDeStock()
    {
        var productos = new[] { P("Medio", 5, 10), P("Casi vacío", 1, 10), P("Vacío", 0, 10), P("Bajo", 3, 10) };

        NivelStock.PorUrgencia(productos).Select(p => p.Nombre).Should().Equal("Vacío", "Casi vacío", "Bajo", "Medio");
    }

    [Test]
    public void PorUrgencia_ConPorcentajeIgual_PasaPrimeroElQueMasUnidadesNecesita_YLuegoAZ()
    {
        var productos = new[]
        {
            P("Zumo", 0, 4),      // 0 %, faltan 4
            P("Harina", 0, 12),   // 0 %, faltan 12  -> primero
            P("Azúcar", 0, 4),    // 0 %, faltan 4   -> empata con Zumo: A–Z
        };

        NivelStock.PorUrgencia(productos).Select(p => p.Nombre).Should().Equal("Harina", "Azúcar", "Zumo");
    }

    [Test]
    public void PorUrgencia_NoIncluyeLosCompletosNiLosSinMaximo()
    {
        var productos = new[] { P("Completo", 10, 10), P("Pasado", 12, 10), P("Sin máximo", 0, 0), P("Falta", 2, 10) };

        NivelStock.PorUrgencia(productos).Select(p => p.Nombre).Should().Equal("Falta");
    }

    // ---------- OrdenAlfabetico ----------

    [Test]
    public void Clave_IgnoraTildesMayusculasYEspacios()
    {
        OrdenAlfabetico.Clave("  Álvaro ").Should().Be("alvaro");
        OrdenAlfabetico.Clave("ÑOÑO").Should().Be("nono");
        OrdenAlfabetico.Clave(null).Should().BeEmpty();
    }

    [Test]
    public void OrdenarAlfabeticamente_ColocaLasTildesJuntoASuLetra()
    {
        var nombres = new[] { "Zumo", "Árbol", "azúcar", "Leche", "Éclair", "Yogur" };

        var ordenados = nombres.OrdenarAlfabeticamente(n => n).ToList();

        ordenados.Should().Equal("Árbol", "azúcar", "Éclair", "Leche", "Yogur", "Zumo");
    }

    [Test]
    public void LuegoAlfabeticamente_DesempataPorNombre()
    {
        var items = new[] { (Grupo: 2, Nombre: "b"), (Grupo: 1, Nombre: "z"), (Grupo: 2, Nombre: "a") };

        var ordenados = items.OrderBy(i => i.Grupo).LuegoAlfabeticamente(i => i.Nombre).Select(i => i.Nombre).ToList();

        ordenados.Should().Equal("z", "a", "b");
    }
}
