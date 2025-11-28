using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators
{
    public class ResetPasswordDTOValidator : AbstractValidator<ResetPasswordDTO>
    {
        public ResetPasswordDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es válido");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("El token es requerido");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contraseña es requerida")
                .MinimumLength(8).WithMessage("La nueva contraseña debe tener al menos 8 caracteres");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("La confirmación de la nueva contraseña es requerida")
                .Equal(x => x.NewPassword).WithMessage("La confirmación no coincide con la nueva contraseña");
        }
    }
}