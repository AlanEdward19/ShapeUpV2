namespace IntegrationTests.Domains.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class MessagingHostedServiceLifecycle
{
    public static IReadOnlyList<IHostedService> Capture(IServiceProvider provider) =>
        provider.GetServices<IHostedService>().ToList();

    public static async Task StartAsync(IReadOnlyList<IHostedService> hostedServices, CancellationToken cancellationToken = default)
    {
        foreach (var hosted in hostedServices)
            await hosted.StartAsync(cancellationToken);
    }

    public static async Task StopAsync(IReadOnlyList<IHostedService> hostedServices, CancellationToken cancellationToken = default)
    {
        for (var i = hostedServices.Count - 1; i >= 0; i--)
            await hostedServices[i].StopAsync(cancellationToken);
    }
}
