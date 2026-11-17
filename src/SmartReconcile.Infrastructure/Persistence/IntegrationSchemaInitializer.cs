using Microsoft.EntityFrameworkCore;

namespace SmartReconcile.Infrastructure.Persistence;

public static class IntegrationSchemaInitializer
{
    public static async Task EnsureCreatedAsync(SmartReconcileDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Tenants" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Name" varchar(200) NOT NULL,
                "Slug" varchar(100) NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tenants_Slug" ON "Tenants" ("Slug");

            CREATE TABLE IF NOT EXISTS "AccountingSettings" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Beraternummer" varchar(7) NOT NULL DEFAULT '1001',
                "Mandantennummer" varchar(5) NOT NULL DEFAULT '10001',
                "ChartOfAccounts" integer NOT NULL DEFAULT 0,
                "RevenueAccount19" varchar(8) NOT NULL DEFAULT '8400',
                "RevenueAccount7" varchar(8) NOT NULL DEFAULT '8300',
                "PaymentClearingAccount" varchar(8) NOT NULL DEFAULT '1360',
                "FeeAccount" varchar(8) NOT NULL DEFAULT '4970',
                "UpdatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "FK_AccountingSettings_Tenants_TenantId"
                    FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AccountingSettings_TenantId"
                ON "AccountingSettings" ("TenantId");

            CREATE TABLE IF NOT EXISTS "IntegrationConnections" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Provider" integer NOT NULL,
                "ExternalStoreId" varchar(200) NOT NULL,
                "DisplayName" varchar(200) NOT NULL,
                "ApiKeyHash" varchar(64) NOT NULL,
                "Status" integer NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "LastSeenAtUtc" timestamp with time zone NULL,
                CONSTRAINT "FK_IntegrationConnections_Tenants_TenantId"
                    FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_IntegrationConnections_ApiKeyHash" ON "IntegrationConnections" ("ApiKeyHash");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_IntegrationConnections_TenantId_Provider_ExternalStoreId"
                ON "IntegrationConnections" ("TenantId", "Provider", "ExternalStoreId");

            CREATE TABLE IF NOT EXISTS "FinancialCases" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CaseNumber" varchar(30) NULL,
                "DiscrepancyId" uuid NULL,
                "Status" integer NOT NULL,
                "Severity" integer NOT NULL,
                "Title" varchar(300) NOT NULL,
                "Description" text NOT NULL,
                "RootCause" text NULL,
                "AffectedAmount" numeric(18,2) NOT NULL,
                "Currency" varchar(3) NOT NULL,
                "DetectedAtUtc" timestamp with time zone NOT NULL,
                "ResolvedAtUtc" timestamp with time zone NULL
            );
            ALTER TABLE "FinancialCases"
                ADD COLUMN IF NOT EXISTS "CaseNumber" varchar(30) NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_FinancialCases_CaseNumber"
                ON "FinancialCases" ("CaseNumber")
                WHERE "CaseNumber" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS "IX_FinancialCases_DiscrepancyId_Status"
                ON "FinancialCases" ("DiscrepancyId", "Status");

            CREATE TABLE IF NOT EXISTS "FinancialCaseLinks" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "FinancialCaseId" uuid NOT NULL,
                "NodeType" integer NOT NULL,
                "EntityId" uuid NOT NULL,
                "Role" varchar(100) NOT NULL,
                "Label" varchar(300) NOT NULL,
                "Amount" numeric(18,2) NULL,
                "Currency" varchar(3) NOT NULL,
                CONSTRAINT "FK_FinancialCaseLinks_FinancialCases_FinancialCaseId"
                    FOREIGN KEY ("FinancialCaseId") REFERENCES "FinancialCases" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_FinancialCaseLinks_Case_Node"
                ON "FinancialCaseLinks" ("FinancialCaseId", "NodeType", "EntityId");

            CREATE TABLE IF NOT EXISTS "FinancialGraphRelations" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "FromNodeType" integer NOT NULL,
                "FromEntityId" uuid NOT NULL,
                "ToNodeType" integer NOT NULL,
                "ToEntityId" uuid NOT NULL,
                "RelationType" integer NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_FinancialGraphRelations_Nodes_Relation"
                ON "FinancialGraphRelations" ("FromNodeType", "FromEntityId", "ToNodeType", "ToEntityId", "RelationType");

            CREATE TABLE IF NOT EXISTS "CaseAuditEvents" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "FinancialCaseId" uuid NOT NULL,
                "EventType" integer NOT NULL,
                "Description" text NOT NULL,
                "Actor" varchar(100) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "FK_CaseAuditEvents_FinancialCases_FinancialCaseId"
                    FOREIGN KEY ("FinancialCaseId") REFERENCES "FinancialCases" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_CaseAuditEvents_FinancialCaseId_CreatedAtUtc"
                ON "CaseAuditEvents" ("FinancialCaseId", "CreatedAtUtc" DESC);

            CREATE TABLE IF NOT EXISTS "WebhookEvents" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "ConnectionId" uuid NOT NULL,
                "ExternalEventId" varchar(200) NOT NULL,
                "EventType" varchar(200) NOT NULL,
                "PayloadJson" text NOT NULL,
                "ReceivedAtUtc" timestamp with time zone NOT NULL,
                "ProcessedAtUtc" timestamp with time zone NULL,
                "IsProcessed" boolean NOT NULL,
                CONSTRAINT "FK_WebhookEvents_Tenants_TenantId"
                    FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_WebhookEvents_Connections_ConnectionId"
                    FOREIGN KEY ("ConnectionId") REFERENCES "IntegrationConnections" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_WebhookEvents_ConnectionId_ExternalEventId"
                ON "WebhookEvents" ("ConnectionId", "ExternalEventId");
            """, cancellationToken);
    }
}
