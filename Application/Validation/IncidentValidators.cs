using FluentValidation;
using Application.DTOs.Incident;

namespace Application.Validation
{
    public class CreateIncidentValidator : AbstractValidator<CreateIncidentRequestDTO>
    {
        public CreateIncidentValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("El titulo es requerido")
                .MinimumLength(2)
                .WithMessage("El titulo debe tener minimo 2 caracteres")
                .MaximumLength(200)
                .WithMessage("El titulo debe tener maximo 200 caracteres");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("La descripcion es requerida")
                .MinimumLength(10)
                .WithMessage("La descripcion debe tener minimo 10 caracteres")
                .MaximumLength(5000)
                .WithMessage("La descripcion debe tener maximo 5000 caracteres");

            RuleFor(x => x.CategoryId)
                .NotEmpty()
                .WithMessage("La categoria es requerida")
                .Must(id => id != Guid.Empty)
                .WithMessage("La categoria debe ser un ID valido");

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 5)
                .WithMessage("La prioridad debe estar entre 1 y 5");
        }
    }

    public class UpdateIncidentValidator : AbstractValidator<UpdateIncidentRequestDTO>
    {
        public UpdateIncidentValidator()
        {
            When(x => !string.IsNullOrEmpty(x.Title), () =>
            {
                RuleFor(x => x.Title)
                    .MinimumLength(2)
                    .WithMessage("El titulo debe tener minimo 2 caracteres")
                    .MaximumLength(200)
                    .WithMessage("El titulo debe tener maximo 200 caracteres");
            });

            When(x => !string.IsNullOrEmpty(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MinimumLength(10)
                    .WithMessage("La descripcion debe tener minimo 10 caracteres")
                    .MaximumLength(5000)
                    .WithMessage("La descripcion debe tener maximo 5000 caracteres");
            });

            RuleFor(x => x.StatusId)
                .InclusiveBetween(1, 5)
                .WithMessage("El estado debe estar entre 1 y 5")
                .When(x => x.StatusId.HasValue);

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 5)
                .WithMessage("La prioridad debe estar entre 1 y 5")
                .When(x => x.Priority.HasValue);
        }
    }

    public class AddCommentValidator : AbstractValidator<AddCommentRequestDTO>
    {
        public AddCommentValidator()
        {
            RuleFor(x => x.Comment)
                .NotEmpty()
                .WithMessage("El comentario es requerido")
                .MinimumLength(1)
                .WithMessage("El comentario debe tener minimo 1 caracter")
                .MaximumLength(5000)
                .WithMessage("El comentario debe tener maximo 5000 caracteres");
        }
    }

    public class AssignUserValidator : AbstractValidator<AssignUserRequestDTO>
    {
        public AssignUserValidator()
        {
            RuleFor(x => x.NewUserId)
                .NotEmpty()
                .WithMessage("El ID del nuevo usuario es requerido")
                .Must(id => id != Guid.Empty)
                .WithMessage("El ID del usuario debe ser valido");
        }
    }
}