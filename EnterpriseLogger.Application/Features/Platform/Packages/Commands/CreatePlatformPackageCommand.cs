using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Packages;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Packages.Dtos;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Packages.Commands;

public class CreatePlatformPackageCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreatePlatformPackageRequest> _validator;
    private readonly IPlatformAuditService _auditService;

    public CreatePlatformPackageCommand(
        IApplicationDbContext context,
        IValidator<CreatePlatformPackageRequest> validator,
        IPlatformAuditService auditService)
    {
        _context = context;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<Result<PlatformPackageDto>> ExecuteAsync(
        CreatePlatformPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<PlatformPackageDto>.Failure($"Validasyon hatası: {errors}");
        }

        var code = request.Code.Trim().ToLowerInvariant();
        var codeExists = await _context.Packages
            .AnyAsync(p => p.Code == code, cancellationToken);

        if (codeExists)
            return Result<PlatformPackageDto>.Failure("Bu paket kodu zaten kullanılıyor.");

        var package = new Domain.Entities.Package
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            AllowedLogLevels = PackageLogLevelHelper.Serialize(
                PackageLogLevelHelper.Parse(request.AllowedLogLevels)),
            IsMailEnabled = request.IsMailEnabled,
            IsSmsEnabled = request.IsSmsEnabled,
            MonthlyRequestLimit = request.MonthlyRequestLimit,
            MaxLogsPerMinute = request.MaxLogsPerMinute,
            StorageRetentionDays = request.StorageRetentionDays,
            PriceMonthly = request.PriceMonthly,
            PriceQuarterly = request.PriceQuarterly,
            PriceSemiAnnual = request.PriceSemiAnnual,
            PriceAnnual = request.PriceAnnual,
            IsDefault = false,
            IsAvailable = request.IsAvailable,
            SortOrder = request.SortOrder,
        };

        _context.Packages.Add(package);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PackageCreated,
            "package",
            package.Id,
            null,
            $"code={package.Code}",
            cancellationToken);

        return Result<PlatformPackageDto>.Success(PlatformPackageMapper.ToDto(package));
    }
}

public class CreatePlatformPackageRequestValidator : AbstractValidator<CreatePlatformPackageRequest>
{
    public CreatePlatformPackageRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Paket kodu boş olamaz.")
            .MaximumLength(32)
            .Must(code => System.Text.RegularExpressions.Regex.IsMatch(
                code.Trim().ToLowerInvariant(),
                "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            .WithMessage("Paket kodu yalnızca küçük harf, rakam ve tire içerebilir.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Paket adı boş olamaz.")
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AllowedLogLevels)
            .Must(PackageLogLevelHelper.IsValidSelection)
            .WithMessage("En az bir geçerli log seviyesi seçilmelidir (INFO, WARNING, ERROR).");
        RuleFor(x => x.MonthlyRequestLimit).GreaterThan(0);
        RuleFor(x => x.MaxLogsPerMinute).GreaterThan(0);
        RuleFor(x => x.StorageRetentionDays).GreaterThan(0);
        RuleFor(x => x.PriceMonthly).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceQuarterly).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceSemiAnnual).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceAnnual).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
