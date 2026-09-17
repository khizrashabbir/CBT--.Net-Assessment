using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Interfaces;

public interface IRegistrationService
{
    Task<RegistrationStartResponse> StartAsync(RegistrationStartRequest request, CancellationToken cancellationToken = default);

    Task<PrivacyPolicyResponse> GetActivePrivacyPolicyAsync(CancellationToken cancellationToken = default);

    Task<RegistrationStepResponse> AcceptPolicyAsync(AcceptPolicyRequest request, CancellationToken cancellationToken = default);

    Task<RegistrationStepResponse> CreatePinAsync(CreatePinRequest request, CancellationToken cancellationToken = default);

    Task<RegistrationStepResponse> SetBiometricAsync(BiometricRequest request, CancellationToken cancellationToken = default);
}
