using EnterpriseLogger.Api.Configuration;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Tenants.Commands;
using EnterpriseLogger.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvFileLoader.LoadIfPresent();

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection is not configured. Set DB_CONNECTION_STRING in .env (see .env.example) " +
        "or ConnectionStrings:DefaultConnection via environment variables / secret store.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, b =>
        b.MigrationsAssembly("EnterpriseLogger.Infrastructure")));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<CreateTenantCommand>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTenantRequestValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
