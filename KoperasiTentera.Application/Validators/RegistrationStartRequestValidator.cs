using FluentValidation;
using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Validators;

public class RegistrationStartRequestValidator : AbstractValidator<RegistrationStartRequest>
{
    public RegistrationStartRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.IcNumber)
            .NotEmpty()
            .Matches("^[0-9]{12}$").WithMessage("IC number must be exactly 12 digits.");

        RuleFor(x => x.MobileNumber)
            .NotEmpty()
            .Matches(@"^\+60[0-9]{8,10}$").WithMessage("Mobile number must be in +60 E.164 format.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);
    }
}
