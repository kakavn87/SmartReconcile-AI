namespace SmartReconcile.Domain.Enums;

public enum DiscrepancyType
{
    MissingPayment,
    OrphanedPayment,
    AmountMismatch,
    DuplicatePayment,
    CurrencyMismatch,
    DuplicateInvoice
}