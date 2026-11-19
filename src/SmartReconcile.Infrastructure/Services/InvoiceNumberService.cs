using System.Data;
using Microsoft.EntityFrameworkCore;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class InvoiceNumberService(SmartReconcileDbContext db)
{
    public async Task<string> NextAsync(Guid tenantId, int year, string prefix, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (prefix is not ("RE" or "ST")) throw new ArgumentException("Unsupported invoice prefix.", nameof(prefix));

        const string sql = """
            INSERT INTO "InvoiceSequences" ("TenantId", "Year", "DocumentPrefix", "NextNumber")
            VALUES (@tenantId, @year, @prefix, 2)
            ON CONFLICT ("TenantId", "Year", "DocumentPrefix")
            DO UPDATE SET "NextNumber" = "InvoiceSequences"."NextNumber" + 1
            RETURNING "NextNumber" - 1;
            """;

        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            var transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            if (transaction is not null)
            {
                command.Transaction = transaction;
            }

            var tenantParameter = command.CreateParameter();
            tenantParameter.ParameterName = "tenantId";
            tenantParameter.Value = tenantId;
            command.Parameters.Add(tenantParameter);

            var yearParameter = command.CreateParameter();
            yearParameter.ParameterName = "year";
            yearParameter.Value = year;
            command.Parameters.Add(yearParameter);

            var prefixParameter = command.CreateParameter();
            prefixParameter.ParameterName = "prefix";
            prefixParameter.Value = prefix;
            command.Parameters.Add(prefixParameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException("Invoice sequence did not return a number.");
            }

            var next = Convert.ToInt32(result);
            return $"{prefix}-{year}-{next:0000}";
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
