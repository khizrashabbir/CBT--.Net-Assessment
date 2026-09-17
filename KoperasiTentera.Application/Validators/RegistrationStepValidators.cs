using FluentValidation;
using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Validators;

public class AcceptPolicyRequestValidator : AbstractValidator<AcceptPolicyRequest>
{
    public AcceptPolicyRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public class CreatePinRequestValidator : AbstractValidator<CreatePinRequest>
{
    public CreatePinRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();

        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches("^[0-9]{6}$").WithMessage("PIN must be exactly 6 digits.");

        RuleFor(x => x.ConfirmPin)
            .NotEmpty()
            .Matches("^[0-9]{6}$").WithMessage("Confirm PIN must be exactly 6 digits.");
    }
}

public class BiometricRequestValidator : AbstractValidator<BiometricRequest>
{
    public BiometricRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
