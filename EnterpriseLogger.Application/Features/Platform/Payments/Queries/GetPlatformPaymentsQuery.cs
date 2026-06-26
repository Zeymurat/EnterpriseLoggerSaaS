using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Payments.Commands;
using EnterpriseLogger.Application.Features.Platform.Payments.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Payments.Queries;

public class GetPlatformPaymentsQuery
{
    private readonly IApplicationDbContext _context;

    public GetPlatformPaymentsQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformPaymentListResponse>> ExecuteAsync(
        PaymentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsNoTracking();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var payments = await query
            .Include(p => p.Tenant)
            .Include(p => p.Package)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var paymentIds = payments.Select(p => p.Id).ToList();
        var linked = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.PaymentId != null && paymentIds.Contains(s.PaymentId.Value))
            .ToDictionaryAsync(s => s.PaymentId!.Value, s => s.Id, cancellationToken);

        var items = payments
            .Select(p => RecordPlatformPaymentCommand.Map(
                p,
                p.Tenant.Name,
                p.Package.Name,
                linked.GetValueOrDefault(p.Id)))
            .ToList();

        return Result<PlatformPaymentListResponse>.Success(new PlatformPaymentListResponse(items));
    }
}

public class GetTenantAvailablePaymentsQuery
{
    private readonly IApplicationDbContext _context;

    public GetTenantAvailablePaymentsQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformPaymentListResponse>> ExecuteAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var linkedPaymentIds = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.PaymentId != null)
            .Select(s => s.PaymentId!.Value)
            .ToListAsync(cancellationToken);

        var payments = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Tenant)
            .Include(p => p.Package)
            .Where(p => p.TenantId == tenantId
                && (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Confirmed)
                && !linkedPaymentIds.Contains(p.Id))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = payments
            .Select(p => RecordPlatformPaymentCommand.Map(p, p.Tenant.Name, p.Package.Name, null))
            .ToList();

        return Result<PlatformPaymentListResponse>.Success(new PlatformPaymentListResponse(items));
    }
}

public class GetTenantPaymentsQuery
{
    private readonly IApplicationDbContext _context;

    public GetTenantPaymentsQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformPaymentListResponse>> ExecuteAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenantExists = await _context.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, cancellationToken);

        if (!tenantExists)
            return Result<PlatformPaymentListResponse>.NotFound("Tenant bulunamadı.");

        var payments = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Tenant)
            .Include(p => p.Package)
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var paymentIds = payments.Select(p => p.Id).ToList();
        var linked = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.PaymentId != null && paymentIds.Contains(s.PaymentId.Value))
            .ToDictionaryAsync(s => s.PaymentId!.Value, s => s.Id, cancellationToken);

        var items = payments
            .Select(p => RecordPlatformPaymentCommand.Map(
                p,
                p.Tenant.Name,
                p.Package.Name,
                linked.GetValueOrDefault(p.Id)))
            .ToList();

        return Result<PlatformPaymentListResponse>.Success(new PlatformPaymentListResponse(items));
    }
}
