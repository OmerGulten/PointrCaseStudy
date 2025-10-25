using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pointr.API;
using Pointr.Infrastructure.Data;

namespace Pointr.Tests
{
    public class TestingFactory : WebApplicationFactory<Program>
    {
        private string _testConnectionString = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, conf) =>
            {
                conf.AddJsonFile("appsettings.json", optional: true);
                conf.AddEnvironmentVariables();
            });

            builder.ConfigureServices(services =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                _testConnectionString = configuration.GetConnectionString("DefaultConnection")!;

                if (string.IsNullOrEmpty(_testConnectionString))
                {
                    throw new InvalidOperationException("TestConnection string not found in configuration.");
                }

                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType.Name.Contains("DbContextOptions")
                ).ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(_testConnectionString,
                        npgsqlOptions =>
                        {
                            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        });
                }, ServiceLifetime.Scoped);

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            });
        }

        public ApplicationDbContext CreateDbContext()
        {
            if (string.IsNullOrEmpty(_testConnectionString))
            {
                throw new InvalidOperationException("TestConnection string not found in configuration.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(_testConnectionString);
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}