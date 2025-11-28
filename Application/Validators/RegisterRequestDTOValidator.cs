using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators
{
    public class RegisterRequestDTOValidator : AbstractValidator<RegisterRequestDTO>
    {
        public RegisterRequestDTOValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("El username es requerido")
                .Length(3, 50).WithMessage("El username debe tener entre 3 y 50 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es válido")
                .MaximumLength(100).WithMessage("El email no puede exceder 100 caracteres");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es requerida")
                .Length(8, 100).WithMessage("La contraseña debe tener entre 8 y 100 caracteres")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
                .WithMessage("La contraseña debe contener al menos: 1 minúscula, 1 mayúscula, 1 número y 1 carácter especial");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirmar contraseña es requerido")
                .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("El apellido es requerido")
                .MaximumLength(50).WithMessage("El apellido no puede exceder 50 caracteres");
        }
    }
}