using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdentityServer4.EntityFramework.Storage;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using System.Security.Claims;
using IdentityModel;
using Server.Data;

namespace Server;

public class SeedData
{
    public static void EnsureSeedData(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AspNetIdentityDbContext>(
            options => options.UseSqlServer(connectionString)
        );

        services.AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<AspNetIdentityDbContext>()
        .AddDefaultTokenProviders();

        services.AddOperationalDbContext(
            options =>
            {
                options.ConfigureDbContext = db =>
                    db.UseSqlServer(
                        connectionString,
                        sql => sql.MigrationsAssembly(typeof(SeedData).Assembly.FullName)
                    );
            }
        );

           services.AddConfigurationDbContext(
            options =>
            {
                options.ConfigureDbContext = db =>
                    db.UseSqlServer(
                        connectionString,
                        sql => sql.MigrationsAssembly(typeof(SeedData).Assembly.FullName)
                    );
            }
        );

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        scope.ServiceProvider.GetService<ConfigurationDbContext>().Database.Migrate();

        var context = scope.ServiceProvider.GetService<ConfigurationDbContext>();
        context.Database.Migrate();

        EnsureSeedData(context);

        var ctx = scope.ServiceProvider.GetService<AspNetIdentityDbContext>();
        ctx.Database.Migrate();
        EnsureUsers(scope);
    }

    private static void EnsureUsers(IServiceScope scope)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var victoria = userManager.FindByNameAsync("victoria").Result;
        if(victoria == null)
        {
            victoria = new IdentityUser
            {
                UserName = "victoria",
                Email = "victoria.secret@keymail.com",
                EmailConfirmed = true
            };

            var result = userManager.CreateAsync(victoria, "Pass123$").Result;

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result = userManager.AddClaimsAsync(
                victoria,
                new Claim[]
                {
                    new Claim(JwtClaimTypes.Name, "Victoria Secret"),
                    new Claim(JwtClaimTypes.GivenName, "Victoria"),
                    new Claim(JwtClaimTypes.FamilyName, "Secret"),
                    new Claim(JwtClaimTypes.WebSite, "http://victoriasecret.com"),
                    new Claim("location", "somewhere")
                }
            ).Result;
                      
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }
        }


    }

    private static void EnsureSeedData(ConfigurationDbContext context)
    {
        if (!context.Clients.Any())
        {
            foreach(var client in Config.Clietns.ToList())
            {
                context.Clients.Add(client.ToEntity());
            }

            context.SaveChanges();
        }
        
        if (!context.IdentityResources.Any())
        {
            foreach(var resource in Config.IdentityResources.ToList())
            {
                context.IdentityResources.Add(resource.ToEntity());                
            }

            context.SaveChanges();
        }

        if (!context.ApiScopes.Any())
        {
            foreach (var scope in Config.ApiScopes.ToList())
            {
                context.ApiScopes.Add(scope.ToEntity());                
            }

            context.SaveChanges();
        }

        if (!context.ApiResources.Any())
        {
            foreach (var resource in Config.ApiResources.ToList())
            {
                context.ApiResources.Add(resource.ToEntity());
            }

            context.SaveChanges();
        }
    }
}