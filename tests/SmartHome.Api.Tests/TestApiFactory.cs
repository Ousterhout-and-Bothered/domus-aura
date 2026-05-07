using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SmartHome.Api.Tests;

/// <summary>
/// Test-specific WebApplicationFactory that locks the host environment to "Test"
/// and overrides config values that depend on infrastructure (Keycloak, HTTPS)
/// not present in the test process. The JWT bearer pipeline still runs — it
/// just doesn't try to fetch HTTPS-only OIDC discovery documents — so anonymous
/// requests are still rejected by the FallbackPolicy.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // The tests don't have a real Keycloak. Allow the JWT bearer
                // handler to initialize against an HTTP authority without
                // throwing on the HTTPS-required check.
                ["Authentication:RequireHttpsMetadata"] = "false",
            });
        });
    }
}