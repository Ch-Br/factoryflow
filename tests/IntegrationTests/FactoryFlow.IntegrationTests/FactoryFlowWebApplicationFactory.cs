using FactoryFlow.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FactoryFlow.IntegrationTests;

public class FactoryFlowWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FactoryFlowDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<FactoryFlowDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthHandler>(
                    IntegrationTestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = IntegrationTestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = IntegrationTestAuthHandler.SchemeName;
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
