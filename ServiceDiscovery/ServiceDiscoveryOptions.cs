namespace ServiceDiscovery;

public class ServiceDiscoveryOptions
{
    public const string SettingSection = "ServiceDiscovery";
    public string ServiceName { get; set; }
    public string HealthCheckPath { get; set; }
    public int HealthCheckInterval { get; set; }
    public string ServiceAddress { get; set; }
    public string ConsulHost { get; set; }
}
