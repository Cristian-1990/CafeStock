namespace CafeStock.Back.Models;

/// <summary>
/// Una hoja de café: la hoja a mano de cada pedido. Se abre al recepcionar café y se cierra al recepcionar el
/// siguiente; las cerradas quedan en el historial. Su número es su Id (Hoja 1, Hoja 2...).
/// </summary>
public record HojaCafe
{
    public int Id { get; init; }
    public DateTime FechaApertura { get; init; }
    public DateTime? FechaCierre { get; init; }
    public bool Abierta => FechaCierre is null;
}

/// <summary>
/// Un paquete físico de café (o un cuarto). Nace al recepcionar, con su fecha de tueste, y comparte código con la
/// hoja en la que llegó (<see cref="HojaOrigenId"/>). Si sobra, sigue estando en las hojas siguientes. Grupo y formato
/// son los que tenía el café al llegar: así una hoja vieja se ve igual aunque el café cambie de grupo.
/// El uso (quién y cuándo) vive en su RegistroCafe, que apunta al paquete y a la hoja en la que se usó.
/// </summary>
public record PaqueteCafe
{
    public int Id { get; init; }
    public int ProductoId { get; init; }
    public string ProductoNombre { get; init; } = string.Empty;
    public GrupoCafe Grupo { get; init; }
    public FormatoCafe Formato { get; init; }
    public int HojaOrigenId { get; init; }
    public DateTime? FechaTueste { get; init; }
    public string RecepcionadoPor { get; init; } = string.Empty;
    public DateTime FechaRecepcion { get; init; }
}

/// <summary>Un paquete que llega en una recepción (el resto de datos los pone el sistema). Grupo y Formato solo hacen falta si el café aún está SinClasificar: se le asignan al recibirlo.</summary>
public record NuevoPaqueteCafe(int ProductoId, DateTime? FechaTueste, GrupoCafe? Grupo = null, FormatoCafe? Formato = null);

/// <summary>Una hoja con su tablero ya construido.</summary>
public record HojaCafeDetalle(HojaCafe Hoja, IReadOnlyList<TarjetaHoja> Tarjetas);

/// <summary>Resumen de una hoja para la lista del historial.</summary>
public record ResumenHojaCafe(HojaCafe Hoja, int Paquetes, int Usados);
