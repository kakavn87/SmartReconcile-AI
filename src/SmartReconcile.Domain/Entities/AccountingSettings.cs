using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Domain.Entities;

public sealed class AccountingSettings
{
    private AccountingSettings() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Beraternummer { get; private set; } = "1001";
    public string Mandantennummer { get; private set; } = "10001";
    public ChartOfAccounts ChartOfAccounts { get; private set; } = ChartOfAccounts.SKR03;
    public string RevenueAccount19 { get; private set; } = "8400";
    public string RevenueAccount7 { get; private set; } = "8300";
    public string PaymentClearingAccount { get; private set; } = "1360";
    public string FeeAccount { get; private set; } = "4970";
    public DateTime UpdatedAtUtc { get; private set; }

    public static AccountingSettings CreateDefault(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        return new AccountingSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string beraternummer,
        string mandantennummer,
        ChartOfAccounts chartOfAccounts,
        string revenueAccount19,
        string revenueAccount7,
        string paymentClearingAccount,
        string feeAccount)
    {
        ValidateNumber(beraternummer, 7, nameof(beraternummer));
        ValidateNumber(mandantennummer, 5, nameof(mandantennummer));
        ValidateAccount(revenueAccount19, nameof(revenueAccount19));
        ValidateAccount(revenueAccount7, nameof(revenueAccount7));
        ValidateAccount(paymentClearingAccount, nameof(paymentClearingAccount));
        ValidateAccount(feeAccount, nameof(feeAccount));

        Beraternummer = beraternummer.Trim();
        Mandantennummer = mandantennummer.Trim();
        ChartOfAccounts = chartOfAccounts;
        RevenueAccount19 = revenueAccount19.Trim();
        RevenueAccount7 = revenueAccount7.Trim();
        PaymentClearingAccount = paymentClearingAccount.Trim();
        FeeAccount = feeAccount.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyChartOfAccountsDefaults(ChartOfAccounts chartOfAccounts)
    {
        ChartOfAccounts = chartOfAccounts;

        if (chartOfAccounts == ChartOfAccounts.SKR03)
        {
            RevenueAccount19 = "8400";
            RevenueAccount7 = "8300";
            PaymentClearingAccount = "1360";
            FeeAccount = "4970";
        }
        else
        {
            RevenueAccount19 = "4400";
            RevenueAccount7 = "4300";
            PaymentClearingAccount = "1460";
            FeeAccount = "6855";
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateNumber(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || !value.All(char.IsDigit))
            throw new ArgumentException("Die Nummer darf nur Ziffern enthalten und die zulässige Länge nicht überschreiten.", parameterName);
    }

    private static void ValidateAccount(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 8 || !value.All(char.IsDigit))
            throw new ArgumentException("Das Konto darf nur Ziffern enthalten und maximal 8 Stellen lang sein.", parameterName);
    }
}
