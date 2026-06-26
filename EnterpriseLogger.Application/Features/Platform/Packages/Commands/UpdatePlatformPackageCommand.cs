using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Packages;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Packages.Dtos;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Packages.Commands;

public class UpdatePlatformPackageCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdatePlatformPackageRequest> _validator;
    private readonly IPlatformAuditService _auditService;

    public UpdatePlatformPackageCommand(
        IApplicationDbContext context,
        IValidator<UpdatePlatformPackageRequest> validator,
        IPlatformAuditService auditService)
    {
        _context = context;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<Result<PlatformPackageDto>> ExecuteAsync(
        int packageId,
        UpdatePlatformPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<PlatformPackageDto>.Failure($"Validasyon hatası: {errors}");
        }

        var package = await _context.Packages.FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);
        if (package is null)
            return Result<PlatformPackageDto>.NotFound("Paket bulunamadı.");

        package.Name = request.Name.Trim();
        package.Description = request.Description.Trim();
        package.AllowedLogLevels = PackageLogLevelHelper.Serialize(
            PackageLogLevelHelper.Parse(request.AllowedLogLevels));
        package.IsMailEnabled = request.IsMailEnabled;
        package.IsSmsEnabled = request.IsSmsEnabled;
        package.MonthlyRequestLimit = request.MonthlyRequestLimit;
        package.MaxLogsPerMinute = request.MaxLogsPerMinute;
        package.StorageRetentionDays = request.StorageRetentionDays;
        package.PriceMonthly = request.PriceMonthly;
        package.PriceQuarterly = request.PriceQuarterly;
        package.PriceSemiAnnual = request.PriceSemiAnnual;
        package.PriceAnnual = request.PriceAnnual;
        package.IsAvailable = request.IsAvailable;
        package.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PackageUpdated,
            "package",
            packageId,
            null,
            $"code={package.Code}",
            cancellationToken);

        return Result<PlatformPackageDto>.Success(PlatformPackageMapper.ToDto(package));
    }
}

public class UpdatePlatformPackageRequestValidator : AbstractValidator<UpdatePlatformPackageRequest>
{
    public UpdatePlatformPackageRequestValidator()
    {
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
