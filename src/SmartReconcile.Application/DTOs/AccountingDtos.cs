using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record AccountingSettingsDto(
    Guid Id,
    Guid TenantId,
    string Beraternummer,
    string Mandantennummer,
    ChartOfAccounts ChartOfAccounts,
    string RevenueAccount19,
    string RevenueAccount7,
    string PaymentClearingAccount,
    string FeeAccount,
    DateTime UpdatedAtUtc);

public sealed record UpdateAccountingSettingsRequest(
    string Beraternummer,
    string Mandantennummer,
    ChartOfAccounts ChartOfAccounts,
    string RevenueAccount19,
    string RevenueAccount7,
    string PaymentClearingAccount,
    string FeeAccount);
