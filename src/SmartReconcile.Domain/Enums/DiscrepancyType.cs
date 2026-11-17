namespace SmartReconcile.Domain.Enums;

public enum DiscrepancyType
{
    MissingPayment,
    OrphanedPayment,
    AmountMismatch,
    DuplicateInvoice
}