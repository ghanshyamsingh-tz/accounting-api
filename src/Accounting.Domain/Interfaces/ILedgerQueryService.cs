namespace Accounting.Domain.Interfaces;

/// <summary>
/// Service interface for querying ledger data (read-only operations)
/// </summary>
public interface ILedgerQueryService
{
    /// <summary>
    /// Gets unbilled ride charges for an account in a billing period
    /// </summary>
    Task<List<RideChargeDto>> GetUnbilledRideChargesAsync(
        Guid accountId,
        Guid tenantId,
        DateTime billingPeriodStart,
        DateTime billingPeriodEnd,
        List<string> excludeRideIds,
        CancellationToken cancellationToken);
}

/// <summary>
/// DTO for ride charge details
/// </summary>
public record RideChargeDto(
    string RideId,
    DateTime RideDate,
    string Description,
    decimal Amount);
