using ConsumerApplication1;
using ConsumerApplication1.Infrastructure;

using IdentityModel.Client;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Polly;

using Refit;

using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetryTracing((tracerBuilder) =>
{
    string jaegerHost = builder.Configuration.GetValue<string>("JaegerAddress:Host");
    int jaegerPort = builder.Configuration.GetValue<int>("JaegerAddress:Port");
    tracerBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Consumer1"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddJaegerExporter(c =>
        {
            c.AgentPort = jaegerPort;
            c.AgentHost = jaegerHost;
        });
});

builder.Services.AddAccessTokenManagement(options =>
    {
        var identityAddress = builder.Configuration.GetValue<string>("IdentityServer:Address");
        var clientId = builder.Configuration.GetValue<string>("IdentityServer:ClientId");
        var clientSecret = builder.Configuration.GetValue<string>("IdentityServer:ClientSecret");
        options.Client.Clients.Add("identityserver", new ClientCredentialsTokenRequest
        {
            Address = identityAddress,
            ClientId = clientId,
            ClientSecret = clientSecret
        });
    })
    .ConfigureBackchannelHttpClient()
    .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5)
        }))
        .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30)
        ));

var retryCount = builder.Configuration.GetValue<int>("Polly:RetryCount");
var retrySleepPowerDuration = builder.Configuration.GetValue<int>("Polly:RetrySleepPowerDuration");
var handledEventsAllowedBeforeBreaking = builder.Configuration.GetValue<int>("Polly:HandledEventsAllowedBeforeBreaking");
var durationOfBreakInSecond = builder.Configuration.GetValue<int>("Polly:DurationOfBreakInSecond");
var apiGatewayAddress = builder.Configuration.GetValue<string>("ApiGatewayAddress");
builder.Services.AddRefitClient<IWeatherForecastClient>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = false
        })
        .ConfigureHttpClient(c =>
        {
            c.BaseAddress = new Uri(apiGatewayAddress);
            c.DefaultRequestHeaders.Clear();
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        })
        .AddClientAccessTokenHandler("identityserver")
        .AddPolicyHandler(PollyPolicies.GetWaitAndRetryPolicy(retryCount, retrySleepPowerDuration))
        .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy(handledEventsAllowedBeforeBreaking, durationOfBreakInSecond));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
