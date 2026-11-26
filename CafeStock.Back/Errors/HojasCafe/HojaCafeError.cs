using CafeStock.Back.Errors.Common;

namespace CafeStock.Back.Errors.HojasCafe;

public abstract record HojaCafeError(string Message) : DomainError(Message)
{
    public sealed record HojaNoEncontrada(int HojaId)
        : HojaCafeError("No se ha encontrado la hoja de café");

    public sealed record Validation(IEnumerable<string> Errors)
        : HojaCafeError($"Errores de validacion:{Environment.NewLine}• {string.Join($"{Environment.NewLine}•", Errors)}");

    public sealed record DatabaseError(string Details)
        : HojaCafeError($"Error de base de datos: {Details}");
}

public static class HojaCafeErrors
{
    public static DomainError HojaNoEncontrada(int hojaId) => new HojaCafeError.HojaNoEncontrada(hojaId);
    public static DomainError Validation(IEnumerable<string> errors) => new HojaCafeError.Validation(errors);
    public static DomainError DatabaseError(string details) => new HojaCafeError.DatabaseError(details);
}
