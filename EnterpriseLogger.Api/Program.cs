using EnterpriseLogger.Api.Configuration;
using EnterpriseLogger.Api.Infrastructure;
using EnterpriseLogger.Api.Middleware;
using EnterpriseLogger.Api.Services;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Auth.Commands;
using EnterpriseLogger.Application.Features.Logs.Commands;
using EnterpriseLogger.Application.Features.Logs.Queries;
using EnterpriseLogger.Application.Features.Tenants.Commands;
using EnterpriseLogger.Application.Features.Users.Commands;
using EnterpriseLogger.Application.Features.Users.Queries;
using EnterpriseLogger.Application.Features.Platform.Packages.Commands;
using EnterpriseLogger.Application.Features.Platform.Packages.Queries;
using EnterpriseLogger.Application.Features.Platform.Auth.Commands;
using EnterpriseLogger.Application.Features.Platform.Tenants.Commands;
using EnterpriseLogger.Application.Features.Platform.Tenants.Queries;
using EnterpriseLogger.Infrastructure.Auth;
using EnterpriseLogger.Infrastructure.Persistence;
using EnterpriseLogger.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvFileLoader.LoadIfPresent();

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

var isTesting = builder.Environment.IsEnvironment("Testing");
var isDevelopment = builder.Environment.IsDevelopment();

if (string.IsNullOrWhiteSpace(connectionString) && !isTesting)
{
    throw new InvalidOperationException(
        "Database connection is not configured. Set DB_CONNECTION_STRING in .env (see .env.example) " +
        "or ConnectionStrings:DefaultConnection via environment variables / secret store.");
}

if (isTesting)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("IntegrationTestsDb"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString!, b =>
            b.MigrationsAssembly("EnterpriseLogger.Infrastructure")));
}

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<IApiKeyHasher, Sha256ApiKeyHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantProvider, HttpContextCurrentTenantProvider>();
builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();
builder.Services.AddScoped<ILoginClientContext, HttpContextLoginClientContext>();

builder.Services.AddDualAuthentication(builder.Environment);
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddRateLimiting(builder.Environment);
builder.Services.AddLoginProtection(builder.Environment);

builder.Services.AddScoped<CreateTenantCommand>();
builder.Services.AddScoped<RotateTenantApiKeyCommand>();
builder.Services.AddScoped<LoginCommand>();
builder.Services.AddScoped<RefreshSessionCommand>();
builder.Services.AddScoped<CreateLogCommand>();
builder.Services.AddScoped<GetLogsQuery>();
builder.Services.AddScoped<ExportLogsQuery>();
builder.Services.AddScoped<GetLogFilterOptionsQuery>();
builder.Services.AddScoped<GetTenantUsersQuery>();
builder.Services.AddScoped<InviteUserCommand>();
builder.Services.AddScoped<UpdateUserPermissionsCommand>();
builder.Services.AddScoped<UpdateUserRoleCommand>();
builder.Services.AddScoped<DeactivateUserCommand>();
builder.Services.AddScoped<PlatformLoginCommand>();
builder.Services.AddScoped<GetPlatformTenantsQuery>();
builder.Services.AddScoped<GetPlatformTenantDetailQuery>();
builder.Services.AddScoped<AssignTenantSubscriptionCommand>();
builder.Services.AddScoped<GetPlatformPackagesQuery>();
builder.Services.AddScoped<UpdatePlatformPackageCommand>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTenantRequestValidator>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description =
            "Tenant API anahtarı veya JWT. Makine entegrasyonu için apiKey; panel için POST /api/auth/login sonrası Bearer token.",
        Name = TenantAuthConstants.ApiKeyHeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT access token. POST /api/auth/login ile alın; değeri Bearer öneki olmadan yapıştırın.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (isTesting)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DatabaseSeeder.SeedAsync(db);
    await PlatformAdminSeeder.SeedTestingAsync(db, hasher);
}

if (isDevelopment)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await PlatformAdminSeeder.SeedDevelopmentAsync(db, hasher);
}

app.UseExceptionHandler();

if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsExtensions.PolicyName);
app.UseHttpsRedirection();
app.UseMiddleware<PublicEndpointRateLimitMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LogIngestRateLimitMiddleware>();
app.MapControllers();

if (isDevelopment || isTesting)
{
    app.MapGet("/api/_debug/throw", (HttpContext _) =>
    {
        throw new InvalidOperationException("ProblemDetails test — Development/Testing only.");
    });
}

app.Run();

public partial class Program;
