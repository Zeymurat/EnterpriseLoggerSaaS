namespace EnterpriseLogger.Api.Configuration;

public static class FrontendCorsExtensions
{
    public const string PolicyName = "Frontend";

    private static readonly string[] DefaultOrigins = ["http://localhost:5173", "http://localhost:5174"];

    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration["CORS_ALLOWED_ORIGINS"]
            ?? Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");

        var allowedOrigins = string.IsNullOrWhiteSpace(origins)
            ? DefaultOrigins
            : origins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
