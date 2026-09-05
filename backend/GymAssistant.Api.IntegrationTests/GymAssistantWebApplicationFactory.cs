using GymAssistant.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
public class GymAssistantWebApplicationFactory : WebApplicationFactory<Program>
{

    private readonly SqliteConnection _sqliteConnection;
    public GymAssistantWebApplicationFactory()
    {
        _sqliteConnection = new SqliteConnection("Data Source=:memory:");
        _sqliteConnection.Open();
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices((services) =>
        {
           var dbContextDescriptor = services.SingleOrDefault(
                x => x.ServiceType ==
                    typeof(IDbContextOptionsConfiguration<AppDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_sqliteConnection);
            });
            
        });
    
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration((config) =>
        {
            var dictionary = new Dictionary<string, string?>
            {
                { "Jwt:Issuer", "https://localhost:5001" },
                { "Jwt:Audience", "https://localhost:5001" },
                { "Jwt:SigningKey", "jakiś-długi-testowy-klucz-minimum-32-bajty" },
            };
            
            config.AddInMemoryCollection(dictionary);
        });
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        dbContext.Database.EnsureCreated();
        return host;
    }
}