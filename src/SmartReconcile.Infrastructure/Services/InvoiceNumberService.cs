using Microsoft.EntityFrameworkCore;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class InvoiceNumberService(SmartReconcileDbContext db)
{
    public async Task<string> NextAsync(Guid tenantId, int year, string prefix, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (prefix is not ("RE" or "ST")) throw new ArgumentException("Unsupported invoice prefix.", nameof(prefix));

        var next = await db.Database.SqlQueryRaw<int>($@"
            INSERT INTO ""InvoiceSequences"" (""TenantId"", ""Year"", ""DocumentPrefix"", ""NextNumber"")
            VALUES ('{tenantId}', {year}, '{prefix}', 2)
            ON CONFLICT (""TenantId"", ""Year"", ""DocumentPrefix"")
            DO UPDATE SET ""NextNumber"" = ""InvoiceSequences"".""NextNumber"" + 1
            RETURNING ""NextNumber"" - 1 AS ""Value""").SingleAsync(cancellationToken);

        return $"{prefix}-{year}-{next:0000}";
    }
}
