namespace CafeStock.Back.Models.Informes;

/// <summary>Chips de rango preestablecido del filtro de "Gasto por proveedor" y "Ticket medio".</summary>
public enum RangoFechas { UltimaSemana, UltimoMes, UltimoAno, Todo }

/// <summary>Punto genérico de una gráfica: etiqueta (proveedor, producto, fecha…) y su valor.</summary>
public record PuntoDato(string Etiqueta, decimal Valor);

/// <summary>PPV de un producto: desviación en € (Valor) y en % respecto al precio de referencia.</summary>
public record PuntoDatoPpv(string Etiqueta, decimal Valor, decimal PorcentajeDesviacion);

/// <summary>Proveedor sin compra reciente; DiasDesdeUltimaCompra null = nunca se le ha comprado.</summary>
public record ProveedorAlerta(string Nombre, int? DiasDesdeUltimaCompra);

/// <summary>Cumplimiento del día de reparto esperado de un proveedor.</summary>
public record InformeReparto(
    string Proveedor, string DiaRepartoEsperado, int TotalCompras, int ComprasEnDiaEsperado,
    decimal PorcentajeCumplimiento, bool Desviacion);

/// <summary>Fila de "Presupuesto vs Real"; SinPresupuesto = hubo gasto pero no se asignó presupuesto.</summary>
public record PresupuestoVsRealFila(
    string Proveedor, decimal? Presupuesto, decimal Real, decimal? PorcentajeDesviacion, bool SinPresupuesto);

/// <summary>Resultado de "Presupuesto vs Real": tabla completa y series de la gráfica (solo con presupuesto).</summary>
public record PresupuestoVsRealResultado(
    List<PresupuestoVsRealFila> Filas, List<PuntoDato> SeriePresupuesto, List<PuntoDato> SerieReal);

/// <summary>Recepción que dejó el stock por encima del máximo.</summary>
public record LineaExcedenteStock(DateTime Fecha, string Producto, string Proveedor, int Cantidad, decimal Excedente);

/// <summary>Gasto del mes actual y variación porcentual frente al mes anterior (null si no hubo gasto el mes anterior).</summary>
public record GastoMensual(decimal GastoMesActual, decimal? VariacionPorcentual);
