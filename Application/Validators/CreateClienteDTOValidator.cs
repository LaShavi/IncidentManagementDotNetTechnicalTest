using Application.DTOs.Cliente;
using FluentValidation;

namespace Application.Validators
{
    public class CreateClienteDTOValidator : AbstractValidator<CreateClienteDTO>
    {
        public CreateClienteDTOValidator()
        {
            RuleFor(x => x.Cedula)
                .NotEmpty().WithMessage("La cédula es requerida")
                .MaximumLength(20).WithMessage("La cédula no puede exceder 20 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es válido")
                .MaximumLength(100).WithMessage("El email no puede exceder 100 caracteres");

            RuleFor(x => x.Telefono)
                .NotEmpty().WithMessage("El teléfono es requerido")
                .MaximumLength(15).WithMessage("El teléfono no puede exceder 15 caracteres");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres");

            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido es requerido")
                .MaximumLength(50).WithMessage("El apellido no puede exceder 50 caracteres");
        }
    }
}