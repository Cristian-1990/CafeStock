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
