using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("WebAPI2", httpClient =>
{
    httpClient.BaseAddress = new Uri("http://localhost:5297");
});

string identityProviderHost = builder.Configuration.GetValue<string>("IdentityProviderHost");
builder.Services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = identityProviderHost;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api1");
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddConsul(builder.Configuration);

builder.Services.AddOpenTelemetryTracing((builder) =>
{
    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebAPI1"))
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = httpContext =>
            {
                return !httpContext.Request.Path.Value?.Contains("/HealthCheck") ?? true;
            };
        })
        .AddHttpClientInstrumentation()
        .AddJaegerExporter(c =>
        {
            c.AgentPort = 6831;
            c.AgentHost = "localhost";
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(app.Configuration.GetValue<string>("ServiceDiscovery:HealthCheckPath"));
app.UseConsulRegisterService();

app.MapControllers().RequireAuthorization("ApiScope");

app.Run();
