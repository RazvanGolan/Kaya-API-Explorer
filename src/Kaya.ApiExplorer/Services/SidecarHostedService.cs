using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaya.ApiExplorer.Services;

public class SidecarHostedService : IHostedService
{
    private readonly IApiExplorerSidecarService _sidecarService;
    private readonly ILogger<SidecarHostedService> _logger;

    public SidecarHostedService(
        IApiExplorerSidecarService sidecarService,
        ILogger<SidecarHostedService> logger)
    {
        _sidecarService = sidecarService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Kaya API Explorer Sidecar Hosted Service");
        await _sidecarService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Kaya API Explorer Sidecar Hosted Service");
        await _sidecarService.StopAsync(cancellationToken);
    }
}
