namespace CafeStock.Back.Services.SnapshotsInventario;

public interface ISnapshotInventarioService
{
    /// <summary>
    /// Si no existe ya un snapshot con Fecha = hoy, calcula el valor total del inventario
    /// (suma de StockActual × PrecioUnitario de todos los productos) y lo guarda. Si ya
    /// existe, no hace nada. No lanza excepciones ni devuelve Result: es una operación de
    /// "asegurar estado" en segundo plano, llamada desde Informes.OnInitializedAsync, que no
    /// debe poder romper la carga de la página si falla (se registra en el log y ya está).
    /// </summary>
    Task AsegurarSnapshotDeHoyAsync();
}
