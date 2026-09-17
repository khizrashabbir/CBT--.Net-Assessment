using FluentValidation;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KoperasiTentera.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IMigrationService, MigrationService>();
        services.AddScoped<IHomeService, HomeService>();

        return services;
    }
}
