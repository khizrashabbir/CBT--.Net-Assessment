using KoperasiTentera.Application.Interfaces;

namespace KoperasiTentera.API.Infrastructure;

/// <summary>
/// Wraps <see cref="IWebHostEnvironment"/> so the Application layer can ask
/// whether OTP codes should be echoed in responses without depending on ASP.NET Core hosting.
/// </summary>
public class AppEnvironment : IAppEnvironment
{
    public AppEnvironment(IWebHostEnvironment webHostEnvironment)
    {
        ReturnOtpInResponse = webHostEnvironment.IsDevelopment();
    }

    public bool ReturnOtpInResponse { get; }
}
