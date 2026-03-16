using Accounting.Identity.Application.Interfaces;
using Accounting.Identity.Domain.Interfaces;
using Accounting.Identity.Infrastructure.Caching;
using Accounting.Identity.Infrastructure.Messaging;
using Accounting.Identity.Infrastructure.Outbox;
using Accounting.Identity.Infrastructure.Persistence;
using Accounting.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Identity.API.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection.
/// Registers all application, infrastructure, and domain services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Identity service dependencies to the service collection.
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("IdentityDb"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("Accounting.Identity.Infrastructure");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                }));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();

        // Outbox Pattern
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();

        // Redis Cache
        var redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var redisInstanceName = configuration["Redis:InstanceName"] ?? "identity:";
        services.AddRedisCache(redisConnectionString, redisInstanceName);

        // Kafka Event Publisher
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaTopic = configuration["Kafka:Topics:DomainEvents"] ?? "identity.domain-events";
        services.AddKafkaEventPublisher(kafkaBootstrapServers, kafkaTopic);

        // MediatR for CQRS
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IOutboxProcessor).Assembly);
        });

        // FluentValidation - validators will be added when command handlers are implemented
        // services.AddValidatorsFromAssembly(typeof(IOutboxProcessor).Assembly);

        // Health Checks
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("IdentityDb") ?? throw new InvalidOperationException("IdentityDb connection string is required"),
                name: "database",
                tags: new[] { "db", "postgresql" })
            .AddRedis(
                redisConnectionString,
                name: "redis-cache",
                tags: new[] { "cache", "redis" });

        return services;
    }

    /// <summary>
    /// Adds authentication and authorization services.
    /// NOTE: JWT authentication commented out until Keycloak is configured
    /// </summary>
    public static IServiceCollection AddIdentityAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Uncomment when Keycloak is configured in environment
        /*
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = configuration["Authentication:Authority"];
                options.Audience = configuration["Authentication:Audience"];
                options.RequireHttpsMetadata = configuration.GetValue<bool>("Authentication:RequireHttpsMetadata", true);

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
        */

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("admin"));
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiting for API endpoints.
    /// NOTE: Commented out until .NET 9 rate limiting APIs are stabilized
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Implement rate limiting once .NET 9 APIs are finalized
        // For now, rely on reverse proxy (nginx/IIS) for rate limiting
        return services;

        /*
        services.AddRateLimiter(options =>
        {
            // Authentication endpoints - strict limit
            options.AddFixedWindowLimiter("authentication", limiterOptions =>
            {
                limiterOptions.PermitLimit = configuration.GetValue<int>("RateLimiting:Authentication:PermitLimit", 5);
                limiterOptions.Window = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:Authentication:WindowSeconds", 10));
                limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0; // No queueing
            });

            // General endpoints - relaxed limit
            options.AddFixedWindowLimiter("general", limiterOptions =>
            {
                limiterOptions.PermitLimit = configuration.GetValue<int>("RateLimiting:General:PermitLimit", 100);
                limiterOptions.Window = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:General:WindowSeconds", 60));
                limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 10;
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9457#section-3.1",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please try again later."
                }, cancellationToken: cancellationToken);
            };
        });

        return services;
        */
    }
}
