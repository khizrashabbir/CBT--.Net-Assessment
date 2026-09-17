using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Interfaces;

public interface IMigrationService
{
    Task<MigrationStartResponse> StartAsync(MigrationStartRequest request, CancellationToken cancellationToken = default);

    Task<ChangeEmailResponse> ChangeEmailAsync(ChangeEmailRequest request, CancellationToken cancellationToken = default);
}
