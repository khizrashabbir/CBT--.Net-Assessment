using System.Net;
using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedPinAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IAppDbContext _dbContext;

    public AuthService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IcLookupResponse> LookupIcAsync(IcLookupRequest request, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.IcNumber == request.IcNumber, cancellationToken);

        if (customer is null)
        {
            return new IcLookupResponse { Status = IcLookupStatus.NotFound };
        }

        if (customer.Status == CustomerStatus.Active)
        {
            return new IcLookupResponse { Status = IcLookupStatus.ActiveUser, CustomerId = customer.Id };
        }

        // Any registered-but-not-yet-active customer (legacy or mid-registration) resumes
        // through the same masked-contact verification path as the migration flow.
        return new IcLookupResponse
        {
            Status = IcLookupStatus.ExistingUserNotMigrated,
            CustomerId = customer.Id,
            MaskedMobileNumber = MaskingHelper.MaskMobile(customer.MobileNumber),
            MaskedEmail = MaskingHelper.MaskEmail(customer.Email)
        };
    }

    public async Task<PinLoginResponse> PinLoginAsync(PinLoginRequest request, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.IcNumber == request.IcNumber, cancellationToken);

        if (customer is null || customer.Status != CustomerStatus.Active || customer.PinHash is null)
        {
            throw new AppException(ErrorCodes.AccountNotFound, "No active account found for this IC number.", HttpStatusCode.NotFound);
        }

        DateTime now = DateTime.UtcNow;
        if (customer.PinLockedUntilUtc is not null && customer.PinLockedUntilUtc > now)
        {
            int retryAfterSeconds = (int)Math.Ceiling((customer.PinLockedUntilUtc.Value - now).TotalSeconds);
            throw new AppException(
                ErrorCodes.AccountLocked,
                $"Too many incorrect PIN attempts. Please try again in {retryAfterSeconds} second(s).",
                HttpStatusCode.Locked,
                new { retryAfterSeconds });
        }

        if (!PinHasher.Verify(request.Pin, customer.PinHash))
        {
            customer.FailedPinAttempts++;
            if (customer.FailedPinAttempts >= MaxFailedPinAttempts)
            {
                customer.PinLockedUntilUtc = now.Add(LockoutDuration);
                customer.FailedPinAttempts = 0;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AppException(ErrorCodes.UnmatchedPin, "The PIN entered is incorrect.", HttpStatusCode.BadRequest);
        }

        customer.FailedPinAttempts = 0;
        customer.PinLockedUntilUtc = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PinLoginResponse
        {
            CustomerId = customer.Id,
            FullName = customer.FullName,
            Status = customer.Status.ToString()
        };
    }
}
