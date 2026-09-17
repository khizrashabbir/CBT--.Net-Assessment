using FluentValidation;
using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Validators;

public class MigrationStartRequestValidator : AbstractValidator<MigrationStartRequest>
{
    public MigrationStartRequestValidator()
    {
        RuleFor(x => x.IcNumber)
            .NotEmpty()
            .Matches("^[0-9]{12}$").WithMessage("IC number must be exactly 12 digits.");
    }
}

public class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);
    }
}
