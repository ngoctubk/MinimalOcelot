
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryApiScopes(builder.Configuration.GetSection("IdentityServer:ApiScopes"))
                .AddInMemoryClients(builder.Configuration.GetSection("IdentityServer:Clients"))
                .AddInMemoryApiResources(builder.Configuration.GetSection("IdentityServer:ApiResources"))
                ;

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseIdentityServer();

app.Run();
