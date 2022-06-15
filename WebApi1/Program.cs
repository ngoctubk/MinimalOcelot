using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Prometheus;

using Serilog;
using Serilog.Enrichers.Span;

using ServiceDiscovery;

using WebApi1;

Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Host.UseSerilog((ctx, lc) =>
    {
        string applicationName = builder.Configuration.GetValue<string>("ApplicationName");
        lc.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithClientIp()
           .Enrich.WithClientAgent()
           .Enrich.WithSpan()
           .Enrich.WithProperty("ApplicationName", applicationName);
    });

    builder.Services.AddDbContext<ApplicationDbContext>(options => 
                options.UseSqlServer(builder.Configuration.GetConnectionString("TestConnection")));    

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
            .AddSqlClientInstrumentation(options => options.SetDbStatementForText = true)
            .AddJaegerExporter(c =>
            {
                c.AgentPort = 6831;
                c.AgentHost = "localhost";
            });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        using var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        identityContext.Database.EnsureCreated();
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks(app.Configuration.GetValue<string>("ServiceDiscovery:HealthCheckPath"));

    app.UseConsulRegisterService();

    app.UseMetricServer();
    app.UseHttpMetrics();

    app.MapControllers().RequireAuthorization("ApiScope");

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}