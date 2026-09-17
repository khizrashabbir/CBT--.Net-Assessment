using FluentValidation;
using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Validators;

public class PinLoginRequestValidator : AbstractValidator<PinLoginRequest>
{
    public PinLoginRequestValidator()
    {
        RuleFor(x => x.IcNumber)
            .NotEmpty()
            .Matches("^[0-9]{12}$").WithMessage("IC number must be exactly 12 digits.");

        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches("^[0-9]{6}$").WithMessage("PIN must be exactly 6 digits.");
    }
}
