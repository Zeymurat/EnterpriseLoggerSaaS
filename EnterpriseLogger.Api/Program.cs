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
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantProvider, HttpContextCurrentTenantProvider>();
builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();

builder.Services.AddJwtAuthentication(builder.Environment);

builder.Services.AddScoped<CreateTenantCommand>();
builder.Services.AddScoped<LoginCommand>();
builder.Services.AddScoped<CreateLogCommand>();
builder.Services.AddScoped<GetLogsQuery>();
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
            "Tenant API anahtarı. Önce POST /api/tenants ile kayıt olun; yanıttaki apiKey değerini buraya yapıştırın. " +
            "Log endpoint'leri (GET/POST /api/logs) için zorunludur.",
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

app.UseExceptionHandler();

if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMappingMiddleware>();
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
