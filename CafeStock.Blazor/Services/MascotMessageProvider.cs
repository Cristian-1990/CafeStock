namespace CafeStock.Blazor.Services;

/// <summary>
/// Claves = ruta relativa EXACTA tal como la declara cada @page (verificado en el código, no
/// asumido — ver MainLayout.razor para el mapeo etiqueta de menú → ruta real). Dos casos no
/// intuitivos: la etiqueta de menú "Productos" apunta a la ruta "lista-detallada", y la
/// etiqueta "Stock" apunta a la ruta "productos" — al revés de lo que sonaría lógico por
/// nombre. Rutas de detalle/edición (p.ej. "productos/5", "productos/nuevo") no están aquí a
/// propósito: no tienen mensaje propio, Get() devuelve null para ellas.
/// </summary>
public class MascotMessageProvider : IMascotMessageProvider
{
    private static readonly Dictionary<string, (MascotPose Pose, string Mensaje)> Mensajes = new()
    {
        [""] = (MascotPose.Saludo,
            "¡Bienvenido a CafeStock! Desde aquí accedes a proveedores, productos, stock y ventas. Vamos paso a paso."),

        ["facturas"] = (MascotPose.Saludo,
            "Aquí está tu historial de facturas. Desde la ficha de cada proveedor también puedes filtrar directo con 'Ver facturas'."),

        ["vender"] = (MascotPose.Celebracion,
            "¡Un café más! Elige el producto y anota quién lo preparó — así llevamos el control de los dos cafés con seguimiento individual."),

        ["lista-compra"] = (MascotPose.Celebracion,
            "¡Pedido en casa! Marca lo recibido y el stock se actualiza solo. Si llega incompleto, no pasa nada — lo que falte se queda pendiente."),

        // Ruta real de la etiqueta de menú "Stock".
        ["productos"] = (MascotPose.Senalando,
            "Mira la columna 'Necesario': ahí ves justo lo que falta reponer. Con 'Generar pedido' te armo el PDF por proveedor."),

        // Ruta real de la etiqueta de menú "Productos".
        ["lista-detallada"] = (MascotPose.Senalando,
            "Aquí tienes cada producto con su foto y stock actual. Toca uno para ver o editar sus datos."),

        ["proveedores"] = (MascotPose.Pensativo,
            "Revisa aquí cada proveedor: contacto, día de reparto, notas y su historial de compras."),

        ["informes"] = (MascotPose.Pensativo,
            "Gasto por proveedor, evolución de precios, comparativas mes a mes — todo el análisis está aquí."),
    };

    public (MascotPose Pose, string Mensaje)? Get(string relativePath) =>
        Mensajes.TryGetValue(relativePath, out var entry) ? entry : null;
}
