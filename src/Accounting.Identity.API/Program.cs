using Accounting.Identity.API.Extensions;
using Accounting.Identity.API.Middleware;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Identity Service")
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/identity-service-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Identity Service");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    // Swagger/OpenAPI
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "Identity Service API",
            Version = "v1",
            Description = "User authentication, login tracking, and security monitoring API"
        });

        // TODO: Add JWT Bearer authentication to Swagger when JWT is configured
    });

    // OpenTelemetry for distributed tracing
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: builder.Configuration["Observability:ServiceName"] ?? "identity-service",
                        serviceVersion: builder.Configuration["Observability:ServiceVersion"] ?? "1.0.0"))
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = (httpContext) => !httpContext.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                // .AddEntityFrameworkCoreInstrumentation() // Requires pre-release package
                .AddSource("Accounting.Identity.*");

            // Export to OTLP endpoint if configured
            var otlpEndpoint = builder.Configuration["Observability:OpenTelemetry:Endpoint"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            }
        });

    // Identity services (database, repositories, caching, messaging)
    builder.Services.AddIdentityServices(builder.Configuration);

    // Authentication & Authorization
    builder.Services.AddIdentityAuthentication(builder.Configuration);

    // Rate limiting
    builder.Services.AddRateLimiting(builder.Configuration);

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API v1");
            options.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
    }

    // Global exception handling
    app.UseExceptionMiddleware();

    // Request logging with Serilog
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null) return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
            return LogEventLevel.Information;
        };
    });

    app.UseHttpsRedirection();
    app.UseCors();
    // app.UseRateLimiter(); // Commented out - see ServiceCollectionExtensions
    // app.UseAuthentication(); // Commented out until JWT is configured
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint
    app.MapHealthChecks("/health");

    Log.Information("Identity Service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
