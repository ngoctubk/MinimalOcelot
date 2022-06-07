using Consul;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ServiceDiscovery;

public static class ConsulRegisterExtensions
{
    public static IApplicationBuilder UseConsulRegisterService(this IApplicationBuilder applicationBuilder)
    {
        var serviceOptions = applicationBuilder.ApplicationServices
                                        .GetRequiredService<IOptions<ServiceDiscoveryOptions>>() 
                                        ?? throw new ArgumentException("Missing dependency", nameof(IOptions<ServiceDiscoveryOptions>));
        var consulClient = applicationBuilder.ApplicationServices
                                        .GetRequiredService<IConsulClient>() 
                                        ?? throw new ArgumentException("Missing dependency", nameof(IConsulClient));
        var lifetime = applicationBuilder.ApplicationServices
                                        .GetRequiredService<IHostApplicationLifetime>()
                                        ?? throw new ArgumentException("Missing dependency", nameof(IHostApplicationLifetime));

        var serviceName = serviceOptions.Value.ServiceName;
        var serviceAddress = new Uri(serviceOptions.Value.ServiceAddress);
        var serviceId = $"{serviceName}_{serviceAddress.Host}:{serviceAddress.Port}";
        var healthCheckUri = new Uri(serviceAddress, serviceOptions.Value.HealthCheckPath).OriginalString;

        var registration = new AgentServiceRegistration()
        {
            ID = serviceId,
            Name = serviceName,
            Address = serviceAddress.Host,
            Port = serviceAddress.Port,

            Check = new AgentCheckRegistration()
            {
                Status = HealthStatus.Passing,
                HTTP = healthCheckUri,
                Interval = TimeSpan.FromSeconds(serviceOptions.Value.HealthCheckInterval)
            }
        };

        consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(true);
        consulClient.Agent.ServiceRegister(registration).ConfigureAwait(true);

        lifetime.ApplicationStopping.Register(() =>
        {
            consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(true);
        });

        return applicationBuilder;
    }
}
