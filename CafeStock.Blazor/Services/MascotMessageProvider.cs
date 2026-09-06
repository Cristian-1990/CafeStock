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
        ["facturas"] = (MascotPose.Albaranes,
            "Aquí consultas el historial de compras con su factura o albarán adjunto. Si buscas las de un proveedor concreto, entra en su ficha y toca 'Ver facturas'."),

        ["vender"] = (MascotPose.Celebracion,
            "¡Un café más! Elige el producto y quién lo preparó — así sabemos cuánto se ha consumido de cada uno."),

        ["lista-compra"] = (MascotPose.Recepcion,
            "Aquí confirmas lo que ha llegado del pedido. Marca las cantidades recibidas y el stock se actualiza solo. Si algo no llega completo, no pasa nada: queda pendiente para la próxima entrega."),

        // Ruta real de la etiqueta de menú "Stock".
        ["productos"] = (MascotPose.Caja,
            "Esta es tu lista de reposición: la columna 'Necesario' te dice qué falta comprar según el máximo de cada producto. Pulsa 'Generar pedido' para el PDF por proveedor."),

        // Ruta real de la etiqueta de menú "Productos".
        ["lista-detallada"] = (MascotPose.Producto,
            "Este es tu catálogo: cada producto con su foto, proveedor y precio de referencia. Toca uno para ver o editar sus datos."),

        ["proveedores"] = (MascotPose.Telefono,
            "Aquí tienes el contacto de cada proveedor: teléfono, día de reparto y notas. También puedes ver su historial de compras desde su ficha."),

        ["informes"] = (MascotPose.Tablet,
            "Aquí analizas cómo va el negocio: gasto por proveedor, evolución de precios y comparativas mes a mes."),
    };

    public (MascotPose Pose, string Mensaje)? Get(string relativePath) =>
        Mensajes.TryGetValue(relativePath, out var entry) ? entry : null;
}
