using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class GymAssistantWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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
        return base.CreateHost(builder);
    }
}