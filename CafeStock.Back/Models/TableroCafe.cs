namespace CafeStock.Back.Models;

/// <summary>
/// Un hueco del tablero de café: representa un paquete físico. Disponible (sin usar) o usado, y en ese caso
/// con el nombre de quien lo registró y cuándo (null si no hay registro, p.ej. stock anterior a la pantalla). RegistroId permite abrir sus detalles y deshacerlo.
/// </summary>
public record HuecoCafe(bool Usado, string? Trabajador, DateTime? Fecha, int? RegistroId = null);

/// <summary>Un café del tablero con sus huecos, de izquierda a derecha: primero los usados y después los disponibles.</summary>
public record TarjetaCafe(Producto Producto, IReadOnlyList<HuecoCafe> Huecos)
{
    public int Total => Huecos.Count;
    public int Disponibles => Huecos.Count(h => !h.Usado);
}

/// <summary>
/// Construye el tablero de la pantalla Café a partir de los productos con grupo y formato y de sus últimos
/// usos (RegistroCafe). Replica la hoja a mano: hay tantos huecos como el stock máximo (o más, si hay más stock
/// del máximo), los disponibles son el stock actual y el resto son paquetes ya usados, con el nombre de quien
/// los registró. No guarda nada: todo sale de datos que ya existen.
/// </summary>
public static class TableroCafe
{
    /// <summary>
    /// Tarjetas ordenadas por formato (paquetes primero, luego cuartos), por grupo (casa, especial,
    /// descafeinado) y por nombre. <paramref name="usos"/> son los últimos registros de cada producto, los más
    /// recientes primero.
    /// </summary>
    public static List<TarjetaCafe> Construir(IEnumerable<Producto> productos, IReadOnlyDictionary<int, List<RegistroCafe>> usos) =>
        productos
            .Where(p => p.GrupoCafe != GrupoCafe.SinGrupo && p.FormatoCafe != FormatoCafe.SinFormato)
            .OrderBy(p => p.FormatoCafe)
            .ThenBy(p => p.GrupoCafe)
            .ThenBy(p => OrdenAlfabetico.Clave(p.Nombre), StringComparer.Ordinal)
            .Select(p => new TarjetaCafe(p, Huecos(p, usos.TryGetValue(p.Id, out var registros) ? registros : [])))
            .ToList();

    /// <summary>Cuántos huecos tiene un café: el stock máximo, o el stock actual si es mayor (nunca se esconde stock real).</summary>
    public static int TotalHuecos(Producto p) => (int)Math.Ceiling(Math.Max(p.StockMaximo, p.StockActual));

    public static List<HuecoCafe> Huecos(Producto p, IReadOnlyList<RegistroCafe> registrosRecientesPrimero)
    {
        var total = TotalHuecos(p);
        var disponibles = (int)Math.Min(total, Math.Floor(Math.Max(0m, p.StockActual)));
        var usados = total - disponibles;

        // Los usados, de más antiguo a más reciente, con su nombre. Si faltan registros (stock anterior a la
        // pantalla), los primeros usados salen sin nombre.
        var conNombre = registrosRecientesPrimero.Take(usados).Reverse().ToList();
        var huecos = new List<HuecoCafe>(total);
        for (var i = 0; i < usados - conNombre.Count; i++)
            huecos.Add(new HuecoCafe(true, null, null));
        huecos.AddRange(conNombre.Select(r => new HuecoCafe(true, r.NombreTrabajador, r.Fecha, r.Id)));
        for (var i = 0; i < disponibles; i++)
            huecos.Add(new HuecoCafe(false, null, null));
        return huecos;
    }
}
