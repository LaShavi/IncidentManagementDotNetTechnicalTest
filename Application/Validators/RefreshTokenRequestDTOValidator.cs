using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators
{
    public class RefreshTokenRequestDTOValidator : AbstractValidator<RefreshTokenRequestDTO>
    {
        public RefreshTokenRequestDTOValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("El refresh token es requerido");
        }
    }
}