using FluentValidation;
using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Validators;

public class OtpSendRequestValidator : AbstractValidator<OtpSendRequest>
{
    public OtpSendRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OtpType).IsInEnum();
    }
}

public class OtpVerifyRequestValidator : AbstractValidator<OtpVerifyRequest>
{
    public OtpVerifyRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OtpType).IsInEnum();
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[0-9]{4}$").WithMessage("OTP code must be exactly 4 digits.");
    }
}
