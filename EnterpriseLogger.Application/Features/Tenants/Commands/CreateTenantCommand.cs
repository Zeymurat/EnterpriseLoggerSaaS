using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Tenants.Dtos;
using EnterpriseLogger.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Tenants.Commands;

public class CreateTenantCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateTenantRequest> _validator;

    public CreateTenantCommand(IApplicationDbContext context, IValidator<CreateTenantRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<Result<TenantResponseDto>> ExecuteAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<TenantResponseDto>.Failure($"Validasyon hatası: {errors}");
        }

        var isApiKeyExists = await _context.Tenants
            .AnyAsync(t => t.ApiKey == request.ApiKey, cancellationToken);

        if (isApiKeyExists)
            return Result<TenantResponseDto>.Failure("Bu API Key sistemde zaten kullanımda.");

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            ApiKey = request.ApiKey.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new TenantResponseDto(
            tenant.Id,
            tenant.Name,
            tenant.ApiKey,
            tenant.IsActive,
            tenant.CreatedAt);

        return Result<TenantResponseDto>.Success(response);
    }
}

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Şirket adı boş olamaz.")
            .MaximumLength(150).WithMessage("Şirket adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API Key alanı zorunludur.")
            .MinimumLength(10).WithMessage("API Key en az 10 karakter olmalıdır.");
    }
}
