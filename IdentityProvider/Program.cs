
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var identityBuilder = builder.Services.AddIdentityServer()
                .AddInMemoryApiScopes(builder.Configuration.GetSection("IdentityServer:ApiScopes"))
                .AddInMemoryClients(builder.Configuration.GetSection("IdentityServer:Clients"))
                .AddInMemoryApiResources(builder.Configuration.GetSection("IdentityServer:ApiResources"));
if (builder.Environment.IsDevelopment())
{
    identityBuilder.AddDeveloperSigningCredential();
}
else
{
    identityBuilder.AddSigningCredential(GetCertificate(builder));
}

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseIdentityServer();

app.Run();

X509Certificate2 GetCertificate(WebApplicationBuilder builder)
{
    string thumbPrint = builder.Configuration["SigningCertificate:CertThumbPrint"];

    using var certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
    certStore.Open(OpenFlags.ReadOnly);
    X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, false);
    if (certCollection.Count == 0)
    {
        throw new Exception("IdentityServer Certificate wasn't found.");
    }
    return certCollection[0];
}