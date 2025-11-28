using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators
{
    public class ChangePasswordDTOValidator : AbstractValidator<ChangePasswordDTO>
    {
        public ChangePasswordDTOValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("La contraseña actual es requerida")
                .MinimumLength(8).WithMessage("La contraseña actual debe tener al menos 8 caracteres");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contraseña es requerida")
                .MinimumLength(8).WithMessage("La nueva contraseña debe tener al menos 8 caracteres");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("La confirmación de la nueva contraseña es requerida")
                .Equal(x => x.NewPassword).WithMessage("La confirmación no coincide con la nueva contraseña");
        }
    }
}