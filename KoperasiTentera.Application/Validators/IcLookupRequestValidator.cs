using FluentValidation;
using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Validators;

public class IcLookupRequestValidator : AbstractValidator<IcLookupRequest>
{
    public IcLookupRequestValidator()
    {
        RuleFor(x => x.IcNumber)
            .NotEmpty()
            .Matches("^[0-9]{12}$").WithMessage("IC number must be exactly 12 digits.");
    }
}
