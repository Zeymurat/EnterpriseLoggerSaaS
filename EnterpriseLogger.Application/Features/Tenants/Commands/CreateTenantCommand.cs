using EnterpriseLogger.Application.Common;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Tenants.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Tenants.Commands;

public class CreateTenantCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateTenantRequest> _validator;
    private readonly IPasswordHasher _passwordHasher;

    public CreateTenantCommand(
        IApplicationDbContext context,
        IValidator<CreateTenantRequest> validator,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _validator = validator;
        _passwordHasher = passwordHasher;
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

        var ownerEmail = request.OwnerEmail.Trim().ToLowerInvariant();

        if (!PhoneNormalizer.TryNormalize(request.OwnerPhone, out var ownerPhone))
            return Result<TenantResponseDto>.Failure("Validasyon hatası: Geçerli bir telefon numarası giriniz.");

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            ApiKeyHash = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var rootUser = new User
        {
            Tenant = tenant,
            Email = ownerEmail,
            Phone = ownerPhone,
            PasswordHash = _passwordHasher.Hash(request.OwnerPassword),
            Role = TenantUserRole.Root,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        _context.Users.Add(rootUser);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<TenantResponseDto>.Failure("Kayıt sırasında bir hata oluştu. Lütfen tekrar deneyin.");
        }

        var response = new TenantResponseDto(
            tenant.Id,
            tenant.Name,
            rootUser.Email,
            rootUser.Phone,
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

        RuleFor(x => x.OwnerEmail)
            .NotEmpty().WithMessage("Sahip e-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256).WithMessage("E-posta adresi en fazla 256 karakter olabilir.");

        RuleFor(x => x.OwnerPhone)
            .NotEmpty().WithMessage("Telefon numarası boş olamaz.")
            .Must(phone => PhoneNormalizer.TryNormalize(phone, out _))
            .WithMessage("Geçerli bir telefon numarası giriniz (ör. +905551234567 veya 05551234567).");

        RuleFor(x => x.OwnerPassword)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");
    }
}
