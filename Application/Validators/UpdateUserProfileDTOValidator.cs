using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators
{
    public class UpdateUserProfileDTOValidator : AbstractValidator<UpdateUserProfileDTO>
    {
        public UpdateUserProfileDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es válido");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("El apellido es requerido")
                .MaximumLength(50).WithMessage("El apellido no puede exceder 50 caracteres");
        }
    }
}