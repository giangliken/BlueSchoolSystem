using BlueSchoolSystem.Models;
using BlueSchoolSystem.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace BlueSchoolSystem.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove DbContextOptions<ApplicationDbContext> (SqlServer)
            var optDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (optDesc != null) services.Remove(optDesc);

            // Remove IDbContextOptionsConfiguration<ApplicationDbContext>
            // (carries the stored SqlServer options action)
            var cfgDescriptors = services
                .Where(d => d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericArguments().Length == 1
                         && d.ServiceType.GetGenericArguments()[0] == typeof(ApplicationDbContext)
                         && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                .ToList();
            foreach (var d in cfgDescriptors) services.Remove(d);

            // Remove IDatabaseProvider registrations (SqlServer provider)
            var provDesc = services
                .Where(d => d.ServiceType.FullName?.EndsWith(".IDatabaseProvider") == true)
                .ToList();
            foreach (var d in provDesc) services.Remove(d);

            // Add fresh InMemory DbContext
            var dbName = $"InMemoryDbForTesting_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });

        // Use "Testing" so Program.cs skips Seeder.SeedAsync
        builder.UseEnvironment("Testing");
    }
}
