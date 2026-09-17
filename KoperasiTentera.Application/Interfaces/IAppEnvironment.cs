namespace KoperasiTentera.Application.Interfaces;

/// <summary>
/// Thin abstraction over the hosting environment so Application-layer services can check
/// whether OTP codes should be echoed back in API responses (Development only, per the plan).
/// </summary>
public interface IAppEnvironment
{
    bool ReturnOtpInResponse { get; }
}
