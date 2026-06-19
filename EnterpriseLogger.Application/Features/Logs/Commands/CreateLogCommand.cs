using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using EnterpriseLogger.Domain.Entities;
using FluentValidation;

namespace EnterpriseLogger.Application.Features.Logs.Commands;

public class CreateLogCommand
{
    private static readonly string[] AllowedLogLevels = ["Info", "Warning", "Error"];

    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;
    private readonly IValidator<CreateLogRequest> _validator;

    public CreateLogCommand(
        IApplicationDbContext context,
        ICurrentTenantProvider tenantProvider,
        IValidator<CreateLogRequest> validator)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _validator = validator;
    }

    public async Task<Result<LogResponseDto>> ExecuteAsync(
        CreateLogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsResolved)
            return Result<LogResponseDto>.Failure("Tenant kimliği çözümlenemedi. X-Api-Key header gerekli.");

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<LogResponseDto>.Failure($"Validasyon hatası: {errors}");
        }

        var log = new SystemLog
        {
            TenantId = _tenantProvider.TenantId!.Value,
            ApplicationName = request.ApplicationName.Trim(),
            LogLevel = NormalizeLogLevel(request.LogLevel),
            Message = request.Message.Trim(),
            Timestamp = DateTime.UtcNow,
            HttpMethod = NormalizeOptional(request.HttpMethod)?.ToUpperInvariant(),
            RequestPath = NormalizeOptional(request.RequestPath),
            StatusCode = request.StatusCode,
            CorrelationId = NormalizeOptional(request.CorrelationId),
            ActorIdentifier = NormalizeOptional(request.ActorIdentifier),
            ExceptionType = NormalizeOptional(request.ExceptionType),
        };

        _context.SystemLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<LogResponseDto>.Success(MapToDto(log));
    }

    internal static LogResponseDto MapToDto(SystemLog log) =>
        new(
            log.Id,
            log.TenantId,
            log.ApplicationName,
            log.LogLevel,
            log.Message,
            log.Timestamp,
            log.HttpMethod,
            log.RequestPath,
            log.StatusCode,
            log.CorrelationId,
            log.ActorIdentifier,
            log.ExceptionType);

    private static string NormalizeLogLevel(string logLevel)
    {
        var trimmed = logLevel.Trim();
        return AllowedLogLevels.First(level =>
            level.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class CreateLogRequestValidator : AbstractValidator<CreateLogRequest>
{
    private static readonly string[] AllowedLogLevels = ["Info", "Warning", "Error"];
    private static readonly string[] AllowedHttpMethods =
        ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];

    public CreateLogRequestValidator()
    {
        RuleFor(x => x.ApplicationName)
            .NotEmpty().WithMessage("Uygulama adı boş olamaz.")
            .MaximumLength(100).WithMessage("Uygulama adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.LogLevel)
            .NotEmpty().WithMessage("Log seviyesi zorunludur.")
            .Must(level => AllowedLogLevels.Contains(level.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Log seviyesi yalnızca Info, Warning veya Error olabilir.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Log mesajı boş olamaz.")
            .MaximumLength(4000).WithMessage("Log mesajı en fazla 4000 karakter olabilir.");

        RuleFor(x => x.HttpMethod)
            .MaximumLength(10)
            .Must(method => method is null || AllowedHttpMethods.Contains(method.Trim().ToUpperInvariant()))
            .WithMessage("HTTP metodu geçersiz.")
            .When(x => !string.IsNullOrWhiteSpace(x.HttpMethod));

        RuleFor(x => x.RequestPath)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.RequestPath));

        RuleFor(x => x.CorrelationId)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.ActorIdentifier)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.ActorIdentifier));

        RuleFor(x => x.ExceptionType)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.ExceptionType));

        RuleFor(x => x.StatusCode)
            .InclusiveBetween(100, 599)
            .When(x => x.StatusCode.HasValue);
    }
}
