namespace CafeStock.Back.Models;

/// <summary>
/// Un hueco del tablero de café: representa un paquete físico. Disponible (sin usar) o usado, y en ese caso
/// con el nombre de quien lo registró y cuándo (null si no hay registro, p.ej. stock anterior a la pantalla). RegistroId permite abrir sus detalles y deshacerlo.
/// </summary>
public record HuecoCafe(bool Usado, string? Trabajador, DateTime? Fecha, int? RegistroId = null,
    PaqueteCafe? Paquete = null, bool PasaASiguiente = false, bool Sobrante = false);

/// <summary>Un café de una hoja (con el grupo y formato que tenía al llegar) y sus huecos: usados primero, luego libres.</summary>
public record TarjetaHoja(int ProductoId, string ProductoNombre, GrupoCafe Grupo, FormatoCafe Formato, IReadOnlyList<HuecoCafe> Huecos)
{
    public int Total => Huecos.Count;
    public int Usados => Huecos.Count(h => h.Usado);
    public int Disponibles => Huecos.Count(h => !h.Usado);
}


/// <summary>
/// Lógica del tablero de café. <see cref="ConstruirHoja"/> pinta una hoja a partir de sus paquetes y usos.
/// <see cref="Huecos"/> y <see cref="TotalHuecos"/> son la regla antigua (huecos = stock máximo, repartidos según el
/// stock actual); solo se usan una vez, para convertir en paquetes el café que ya tenía stock antes de las hojas.
/// </summary>
public static class TableroCafe
{
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

    /// <summary>
    /// Tablero de una hoja. Un paquete está en la hoja H si llegó en ella o antes (HojaOrigenId &lt;= H) y no se usó antes
    /// de H (sin uso, o usado en H o después). Si se usó en H sale usado con su nombre; si se usó en una hoja posterior
    /// sale libre, marcado "pasa a la siguiente", porque la hoja cerrada queda como estaba. Los que llegaron antes
    /// que H salen marcados como sobrantes. <paramref name="usoPorPaquete"/> es el RegistroCafe de cada paquete usado.
    /// </summary>
    public static List<TarjetaHoja> ConstruirHoja(int hojaId, IEnumerable<PaqueteCafe> paquetes, IReadOnlyDictionary<int, RegistroCafe> usoPorPaquete)
    {
        var presentes = new List<(PaqueteCafe Paquete, RegistroCafe? Uso)>();
        foreach (var paquete in paquetes)
        {
            if (paquete.HojaOrigenId > hojaId) continue;
            usoPorPaquete.TryGetValue(paquete.Id, out var uso);
            if (uso?.HojaId is int hojaUso && hojaUso < hojaId) continue;
            presentes.Add((paquete, uso));
        }

        return presentes
            .GroupBy(x => (x.Paquete.Formato, x.Paquete.Grupo, x.Paquete.ProductoId, x.Paquete.ProductoNombre))
            .OrderBy(g => g.Key.Formato)
            .ThenBy(g => g.Key.Grupo)
            .ThenBy(g => OrdenAlfabetico.Clave(g.Key.ProductoNombre), StringComparer.Ordinal)
            .Select(g =>
            {
                // Usado en esta hoja (o sin hoja anotada); usados en una hoja posterior cuentan como libres.
                bool UsadoAqui((PaqueteCafe Paquete, RegistroCafe? Uso) x) => x.Uso is not null && (x.Uso.HojaId is null || x.Uso.HojaId == hojaId);
                var usados = g.Where(UsadoAqui)
                    .OrderBy(x => x.Uso!.Fecha).ThenBy(x => x.Paquete.Id)
                    .Select(x => Hueco(x.Paquete, x.Uso, hojaId, pasa: false));
                var libres = g.Where(x => !UsadoAqui(x))
                    .OrderBy(x => x.Paquete.HojaOrigenId).ThenBy(x => x.Paquete.Id)
                    .Select(x => Hueco(x.Paquete, null, hojaId, pasa: x.Uso is not null));
                return new TarjetaHoja(g.Key.ProductoId, g.Key.ProductoNombre, g.Key.Grupo, g.Key.Formato, usados.Concat(libres).ToList());
            })
            .ToList();
    }

    private static HuecoCafe Hueco(PaqueteCafe paquete, RegistroCafe? uso, int hojaId, bool pasa) =>
        new(uso is not null, uso?.NombreTrabajador, uso?.Fecha, uso?.Id, paquete, pasa, paquete.HojaOrigenId < hojaId);
}
