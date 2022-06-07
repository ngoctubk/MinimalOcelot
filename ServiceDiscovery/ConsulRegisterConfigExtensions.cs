using Consul;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceDiscovery;

public static class ConsulRegisterConfigExtensions
{
    public static IServiceCollection AddConsul(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceDiscoveryOptions>(configuration.GetSection(ServiceDiscoveryOptions.SettingSection));

        services.AddSingleton<IConsulClient>(p => new ConsulClient(consulConfig =>
        {
            var host = configuration.GetValue<string>("ServiceDiscovery:ConsulHost");
            consulConfig.Address = new Uri(host);
        }));
        return services;
    }

}
