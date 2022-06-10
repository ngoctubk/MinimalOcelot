using Microsoft.IdentityModel.Tokens;

using Ocelot.Administration;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Enrichers.Span;

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

    string environmentName = builder.Environment.EnvironmentName;
    builder.Configuration.AddJsonFile($"ocelot.{environmentName}.json", true, true);

    //builder.Services.AddSingleton<ITracer>(sp =>
    //{
    //    var loggerFactory = sp.GetService<ILoggerFactory>();
    //    var resolver = new SenderResolver(loggerFactory).RegisterSenderFactory<ThriftSenderFactory>();
    //    Configuration.SenderConfiguration senderConfiguration = new Configuration.SenderConfiguration(loggerFactory)
    //        .WithSenderResolver(resolver);

    //    var tracer = new Tracer.Builder("MinimalOcelot")
    //    .WithSampler(new ConstSampler(true))
    //    .WithReporter(new RemoteReporter.Builder().WithSender(senderConfiguration.GetSender()).Build())
    //    .Build();

    //    GlobalTracer.Register(tracer);
    //    return tracer;
    //});

    builder.Services.AddOcelot()
        .AddCacheManager(x => x.WithDictionaryHandle())
        .AddPolly()
        .AddConsul()
        //.AddOpenTracing()
        .AddAdministration(builder.Configuration.GetValue<string>("AdministrationPath"), options =>
        {
            options.Authority = builder.Configuration.GetValue<string>("AdministrationAuthentication:Authority");
            options.Audience = builder.Configuration.GetValue<string>("AdministrationAuthentication:Audience");
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
            };
        });

    builder.Services.AddOpenTelemetryTracing((builder) =>
    {
        builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MinimalOcelot"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(c =>
            {
                c.AgentPort = 6831;
                c.AgentHost = "localhost";
            });
    });

    builder.Services.AddAuthentication()
        .AddJwtBearer("IdpBearer", options =>
        {
            options.Authority = builder.Configuration.GetValue<string>("RoutesAuthentication:Authority");
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
            };
        });

    var app = builder.Build();

    // Configure the HTTP request pipeline.

    app.UseHttpsRedirection();

    app.UseSerilogRequestLogging();

    app.MapWhen(context => context.Request.Path.StartsWithSegments(app.Configuration.GetValue<string>("HealthCheckPath"))
            , (appBuilder) =>
                            {
                                appBuilder.Run(async (context) =>
                                {
                                    await context.Response.WriteAsync("API Gateway");
                                });
                            });
    app.UseAuthentication();
    app.UseOcelot().Wait();

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