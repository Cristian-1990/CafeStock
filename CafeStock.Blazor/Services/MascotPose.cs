namespace CafeStock.Blazor.Services;

/// <summary>
/// Pose/expresión de la mascota — una imagen distinta por valor. Nunca comparar por el nombre
/// de archivo o un string "saludo" a pelo; el enum es la única fuente de verdad sobre qué pose
/// corresponde a cada ruta (ver MascotMessageProvider) y qué imagen se muestra (ver AppMascot).
/// </summary>
public enum MascotPose
{
    Saludo,
    Celebracion,
    Senalando,
    Pensativo
}
