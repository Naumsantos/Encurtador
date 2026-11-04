using Encurtador.Application.Persistence;
using Encurtador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Encurtador.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLinkInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Links")
            ?? throw new InvalidOperationException("ConnectionStrings:Links não foi configurada.");

        services.AddDbContext<LinksDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ILinkStore, EfLinkStore>();
        return services;
    }
}
