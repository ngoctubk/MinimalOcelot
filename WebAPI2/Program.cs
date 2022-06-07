using Microsoft.Extensions.Options;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddConsul(builder.Configuration);

builder.Services.AddOpenTelemetryTracing((builder) =>
                {
                    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebAPI2"))
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = httpContext =>
                            {
                                return httpContext.Request.Path.Value == null ? true : !httpContext.Request.Path.Value.Contains("/HealthCheck");
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

app.UseAuthorization();

app.MapHealthChecks(app.Configuration.GetValue<string>("ServiceDiscovery:HealthCheckPath"));
app.UseConsulRegisterService();

app.MapControllers();

app.Run();
