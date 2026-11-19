namespace SmartReconcile.Application.DTOs;

public sealed record InvoiceSettingsDto(
    bool AutomaticInvoiceCreationEnabled,
    string CompanyName,
    string CompanyAddress,
    string TaxNumber,
    string VatId,
    string BankDetails,
    string? InvoiceAccessTokenLast4,
    DateTime? InvoiceAccessTokenUpdatedAtUtc);

public sealed record UpdateInvoiceSettingsRequest(
    bool AutomaticInvoiceCreationEnabled,
    string CompanyName,
    string CompanyAddress,
    string TaxNumber,
    string VatId,
    string BankDetails);

public sealed record InvoiceAccessTokenDto(
    string Token,
    string TokenLast4,
    DateTime UpdatedAtUtc,
    string LiquidSnippet);
