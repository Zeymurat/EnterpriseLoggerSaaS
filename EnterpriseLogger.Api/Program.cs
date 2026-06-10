using EnterpriseLogger.Api.Configuration;
using EnterpriseLogger.Api.Middleware;
using EnterpriseLogger.Api.Services;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Logs.Commands;
using EnterpriseLogger.Application.Features.Logs.Queries;
using EnterpriseLogger.Application.Features.Tenants.Commands;
using EnterpriseLogger.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvFileLoader.LoadIfPresent();

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

var isTesting = builder.Environment.IsEnvironment("Testing");

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantProvider, HttpContextCurrentTenantProvider>();

builder.Services.AddScoped<CreateTenantCommand>();
builder.Services.AddScoped<CreateLogCommand>();
builder.Services.AddScoped<GetLogsQuery>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTenantRequestValidator>();

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
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<TenantMappingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
