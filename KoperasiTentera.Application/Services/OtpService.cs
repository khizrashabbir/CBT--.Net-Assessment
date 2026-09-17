using System.Net;
using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Application.Services;

public class OtpService : IOtpService
{
    private const int OtpLengthDigits = 4;
    private static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(120);
    private const int MaxFailedAttempts = 3;

    private readonly IAppDbContext _dbContext;
    private readonly IAppEnvironment _environment;

    public OtpService(IAppDbContext dbContext, IAppEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<OtpSendResponse> SendAsync(OtpSendRequest request, CancellationToken cancellationToken = default)
    {
        Customer customer = await GetCustomerOrThrowAsync(request.CustomerId, cancellationToken);
        OtpType otpType = Map(request.OtpType);

        OtpVerification? lastOtp = await _dbContext.OtpVerifications
            .Where(o => o.CustomerId == customer.Id && o.OtpType == otpType)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        if (lastOtp is not null)
        {
            TimeSpan elapsed = now - lastOtp.CreatedAtUtc;
            if (elapsed < ResendCooldown)
            {
                int retryAfterSeconds = (int)Math.Ceiling((ResendCooldown - elapsed).TotalSeconds);
                throw new AppException(
                    ErrorCodes.ResendNotAllowed,
                    $"Please wait {retryAfterSeconds} second(s) before requesting a new OTP.",
                    HttpStatusCode.TooManyRequests,
                    new { retryAfterSeconds });
            }

            if (!lastOtp.IsUsed)
            {
                lastOtp.IsUsed = true;
            }
        }

        OtpVerification otp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = otpType,
            Code = GenerateOtpCode(),
            ExpiresAtUtc = now.Add(OtpValidity),
            IsUsed = false,
            FailedAttempts = 0,
            CreatedAtUtc = now
        };

        _dbContext.OtpVerifications.Add(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OtpSendResponse
        {
            ExpiresAtUtc = otp.ExpiresAtUtc,
            OtpCode = _environment.ReturnOtpInResponse ? otp.Code : null
        };
    }

    public async Task<OtpVerifyResponse> VerifyAsync(OtpVerifyRequest request, CancellationToken cancellationToken = default)
    {
        Customer customer = await GetCustomerOrThrowAsync(request.CustomerId, cancellationToken);
        OtpType otpType = Map(request.OtpType);

        OtpVerification? otp = await _dbContext.OtpVerifications
            .Where(o => o.CustomerId == customer.Id && o.OtpType == otpType && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            throw new AppException(ErrorCodes.OtpExpired, "No active OTP found for this request. Please resend.", HttpStatusCode.BadRequest);
        }

        if (otp.FailedAttempts >= MaxFailedAttempts)
        {
            throw new AppException(ErrorCodes.OtpMaxAttempts, "Maximum OTP attempts reached. Please request a new code.", HttpStatusCode.BadRequest);
        }

        if (otp.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new AppException(ErrorCodes.OtpExpired, "This OTP has expired. Please request a new code.", HttpStatusCode.BadRequest);
        }

        if (!string.Equals(otp.Code, request.Code, StringComparison.Ordinal))
        {
            otp.FailedAttempts++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AppException(ErrorCodes.IncorrectOtp, "The OTP entered is incorrect.", HttpStatusCode.BadRequest);
        }

        otp.IsUsed = true;
        AdvanceCustomerStatus(customer, otpType);
        customer.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OtpVerifyResponse
        {
            CustomerId = customer.Id,
            Status = customer.Status.ToString()
        };
    }

    private static void AdvanceCustomerStatus(Customer customer, OtpType otpType)
    {
        if (otpType == OtpType.Mobile)
        {
            if (customer.Status != CustomerStatus.PendingVerification)
            {
                throw new AppException(ErrorCodes.InvalidState, "Mobile OTP cannot be verified in the current state.", HttpStatusCode.Conflict);
            }

            customer.Status = CustomerStatus.MobileVerified;
        }
        else
        {
            if (customer.Status != CustomerStatus.MobileVerified)
            {
                throw new AppException(ErrorCodes.InvalidState, "Mobile number must be verified before email OTP.", HttpStatusCode.Conflict);
            }

            customer.Status = CustomerStatus.EmailVerified;
        }
    }

    private async Task<Customer> GetCustomerOrThrowAsync(Guid customerId, CancellationToken cancellationToken)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
        {
            throw new AppException(ErrorCodes.AccountNotFound, "Customer not found.", HttpStatusCode.NotFound);
        }

        return customer;
    }

    private static OtpType Map(OtpTypeDto otpType) => otpType switch
    {
        OtpTypeDto.Mobile => OtpType.Mobile,
        OtpTypeDto.Email => OtpType.Email,
        _ => throw new ArgumentOutOfRangeException(nameof(otpType), otpType, null)
    };

    private static string GenerateOtpCode() => Random.Shared.Next(0, 10_000).ToString($"D{OtpLengthDigits}");
}
