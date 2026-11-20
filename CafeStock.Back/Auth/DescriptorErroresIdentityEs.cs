using Microsoft.AspNetCore.Identity;

namespace CafeStock.Back.Auth;

/// <summary>Mensajes de error de Identity en castellano (por defecto vienen en inglés).</summary>
public class DescriptorErroresIdentityEs : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "Ha ocurrido un error desconocido.");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), $"El usuario \"{userName}\" ya existe.");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), $"El nombre de usuario \"{userName}\" no es válido: solo puede tener letras, números y los símbolos - . _ @ +");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "La contraseña debe tener al menos un número.");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "La contraseña debe tener al menos una minúscula.");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "La contraseña debe tener al menos una mayúscula.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe tener al menos un símbolo.");
    public override IdentityError UserAlreadyInRole(string role) => Error(nameof(UserAlreadyInRole), $"El usuario ya tiene el rol {role}.");
    public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure), "Otra persona modificó este usuario a la vez. Vuelve a intentarlo.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
