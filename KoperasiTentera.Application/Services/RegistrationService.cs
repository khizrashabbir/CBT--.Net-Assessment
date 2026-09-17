using System.Net;
using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Application.Services;

public class RegistrationService : IRegistrationService
{
    private readonly IAppDbContext _dbContext;
    private readonly IOtpService _otpService;

    public RegistrationService(IAppDbContext dbContext, IOtpService otpService)
    {
        _dbContext = dbContext;
        _otpService = otpService;
    }

    public async Task<RegistrationStartResponse> StartAsync(RegistrationStartRequest request, CancellationToken cancellationToken = default)
    {
        bool exists = await _dbContext.Customers.AnyAsync(c => c.IcNumber == request.IcNumber, cancellationToken);
        if (exists)
        {
            throw new AppException(ErrorCodes.AccountAlreadyExists, "An account already exists for this IC number.", HttpStatusCode.Conflict);
        }

        DateTime now = DateTime.UtcNow;
        Customer customer = new()
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            IcNumber = request.IcNumber,
            MobileNumber = request.MobileNumber,
            Email = request.Email,
            CustomerType = CustomerType.NewCustomer,
            Status = CustomerStatus.PendingVerification,
            IsBiometricEnabled = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        OtpSendResponse otpResponse = await _otpService.SendAsync(
            new OtpSendRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile },
            cancellationToken);

        return new RegistrationStartResponse
        {
            CustomerId = customer.Id,
            Status = customer.Status.ToString(),
            MobileOtpExpiresAtUtc = otpResponse.ExpiresAtUtc,
            MobileOtpCode = otpResponse.OtpCode
        };
    }

    public async Task<PrivacyPolicyResponse> GetActivePrivacyPolicyAsync(CancellationToken cancellationToken = default)
    {
        PrivacyPolicy? policy = await _dbContext.PrivacyPolicies
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            throw new AppException(ErrorCodes.InvalidState, "No active privacy policy is configured.", HttpStatusCode.InternalServerError);
        }

        return new PrivacyPolicyResponse { Version = policy.Version, Content = policy.Content };
    }

    public async Task<RegistrationStepResponse> AcceptPolicyAsync(AcceptPolicyRequest request, CancellationToken cancellationToken = default)
    {
        Customer customer = await GetCustomerOrThrowAsync(request.CustomerId, cancellationToken);

        if (customer.Status != CustomerStatus.EmailVerified)
        {
            throw new AppException(ErrorCodes.InvalidState, "Email must be verified before accepting the privacy policy.", HttpStatusCode.Conflict);
        }

        PrivacyPolicyResponse activePolicy = await GetActivePrivacyPolicyAsync(cancellationToken);

        customer.PolicyAcceptedAtUtc = DateTime.UtcNow;
        customer.AcceptedPolicyVersion = activePolicy.Version;
        customer.Status = CustomerStatus.PolicyAccepted;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegistrationStepResponse { CustomerId = customer.Id, Status = customer.Status.ToString() };
    }

    public async Task<RegistrationStepResponse> CreatePinAsync(CreatePinRequest request, CancellationToken cancellationToken = default)
    {
        Customer customer = await GetCustomerOrThrowAsync(request.CustomerId, cancellationToken);

        if (customer.Status != CustomerStatus.PolicyAccepted)
        {
            throw new AppException(ErrorCodes.InvalidState, "Privacy policy must be accepted before creating a PIN.", HttpStatusCode.Conflict);
        }

        if (!string.Equals(request.Pin, request.ConfirmPin, StringComparison.Ordinal))
        {
            throw new AppException(ErrorCodes.UnmatchedPin, "PIN and confirmation PIN do not match.", HttpStatusCode.BadRequest);
        }

        customer.PinHash = PinHasher.Hash(request.Pin);
        customer.Status = CustomerStatus.PinCreated;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegistrationStepResponse { CustomerId = customer.Id, Status = customer.Status.ToString() };
    }

    public async Task<RegistrationStepResponse> SetBiometricAsync(BiometricRequest request, CancellationToken cancellationToken = default)
    {
        Customer customer = await GetCustomerOrThrowAsync(request.CustomerId, cancellationToken);

        if (customer.Status != CustomerStatus.PinCreated)
        {
            throw new AppException(ErrorCodes.InvalidState, "PIN must be created before completing onboarding.", HttpStatusCode.Conflict);
        }

        customer.IsBiometricEnabled = request.Enable;
        customer.Status = CustomerStatus.Active;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegistrationStepResponse { CustomerId = customer.Id, Status = customer.Status.ToString() };
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
}
