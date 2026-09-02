using Microsoft.AspNetCore.Identity;

namespace GymAssistant.Api.Authentication;

public sealed class PolishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = "Nazwa użytkownika jest nieprawidłowa."
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = "Podaj prawidłowy adres email."
    };

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = "Użytkownik o podanej nazwie już istnieje."
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = "Konto z tym adresem email już istnieje."
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = $"Hasło musi mieć co najmniej {length} znaków."
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = $"Hasło musi zawierać co najmniej {uniqueChars} różnych znaków."
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = "Hasło musi zawierać co najmniej jeden znak specjalny."
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = "Hasło musi zawierać co najmniej jedną cyfrę."
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = "Hasło musi zawierać co najmniej jedną małą literę."
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = "Hasło musi zawierać co najmniej jedną wielką literę."
    };
}
