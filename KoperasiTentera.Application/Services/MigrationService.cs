using System.Net;
using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Application.Services;

public class MigrationService : IMigrationService
{
    private readonly IAppDbContext _dbContext;
    private readonly IOtpService _otpService;

    public MigrationService(IAppDbContext dbContext, IOtpService otpService)
    {
        _dbContext = dbContext;
        _otpService = otpService;
    }

    public async Task<MigrationStartResponse> StartAsync(MigrationStartRequest request, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.IcNumber == request.IcNumber && c.CustomerType == CustomerType.ExistingUser, cancellationToken);

        if (customer is null)
        {
            throw new AppException(ErrorCodes.AccountNotFound, "No legacy account found for this IC number.", HttpStatusCode.NotFound);
        }

        if (customer.Status == CustomerStatus.Active)
        {
            throw new AppException(ErrorCodes.InvalidState, "This account has already been migrated.", HttpStatusCode.Conflict);
        }

        OtpSendResponse otpResponse = await _otpService.SendAsync(
            new OtpSendRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile },
            cancellationToken);

        return new MigrationStartResponse
        {
            CustomerId = customer.Id,
            MaskedMobileNumber = MaskingHelper.MaskMobile(customer.MobileNumber),
            MaskedEmail = MaskingHelper.MaskEmail(customer.Email),
            MobileOtpExpiresAtUtc = otpResponse.ExpiresAtUtc,
            MobileOtpCode = otpResponse.OtpCode
        };
    }

    public async Task<ChangeEmailResponse> ChangeEmailAsync(ChangeEmailRequest request, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new AppException(ErrorCodes.AccountNotFound, "Customer not found.", HttpStatusCode.NotFound);
        }

        if (customer.Status == CustomerStatus.Active)
        {
            throw new AppException(ErrorCodes.InvalidState, "This account has already been migrated.", HttpStatusCode.Conflict);
        }

        customer.Email = request.NewEmail;
        customer.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        OtpSendResponse otpResponse = await _otpService.SendAsync(
            new OtpSendRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Email },
            cancellationToken);

        return new ChangeEmailResponse
        {
            MaskedEmail = MaskingHelper.MaskEmail(customer.Email),
            EmailOtpExpiresAtUtc = otpResponse.ExpiresAtUtc,
            EmailOtpCode = otpResponse.OtpCode
        };
    }
}
