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

            INSERT INTO "Tenants" ("Id", "Name", "Slug", "IsActive", "CreatedAtUtc")
            VALUES (
                '00000000-0000-0000-0000-000000000001',
                'SmartReconcile Demo',
                'smartreconcile-demo',
                TRUE,
                NOW()
            )
            ON CONFLICT ("Id") DO NOTHING;

            ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "TaxRate" numeric(5,2) NOT NULL DEFAULT 19;
            ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';
            UPDATE "Orders"
            SET "TenantId" = '00000000-0000-0000-0000-000000000001'
            WHERE "TenantId" IS NULL;
            ALTER TABLE "Orders"
                DROP CONSTRAINT IF EXISTS "FK_Orders_Tenants_TenantId";
            ALTER TABLE "Orders"
                ADD CONSTRAINT "FK_Orders_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            DROP INDEX IF EXISTS "IX_Orders_OrderNumber";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_TenantId_OrderNumber"
                ON "Orders" ("TenantId", "OrderNumber");
            CREATE INDEX IF NOT EXISTS "IX_Orders_TenantId"
                ON "Orders" ("TenantId");

            ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "FeeAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "Kind" integer NOT NULL DEFAULT 0;
            ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "Gateway" varchar(200);
            ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "Currency" varchar(3) NOT NULL DEFAULT 'EUR';
            -- Tenant isolation migration for legacy tables. Existing legacy rows belonged
            -- to the original demo tenant; where a relationship exists, prefer its owner.
            ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
            UPDATE "Payments" p
            SET "TenantId" = o."TenantId"
            FROM "Orders" o
            WHERE p."OrderId" = o."Id" AND p."TenantId" IS NULL;
            UPDATE "Payments"
            SET "TenantId" = '00000000-0000-0000-0000-000000000001'
            WHERE "TenantId" IS NULL;
            ALTER TABLE "Payments" ALTER COLUMN "TenantId" SET NOT NULL;
            ALTER TABLE "Payments" DROP CONSTRAINT IF EXISTS "FK_Payments_Tenants_TenantId";
            ALTER TABLE "Payments" ADD CONSTRAINT "FK_Payments_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            DROP INDEX IF EXISTS "IX_Payments_TransactionId";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payments_TenantId_TransactionId" ON "Payments" ("TenantId", "TransactionId");
            CREATE INDEX IF NOT EXISTS "IX_Payments_TenantId" ON "Payments" ("TenantId");

            ALTER TABLE "Invoices" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
            UPDATE "Invoices" SET "TenantId" = '00000000-0000-0000-0000-000000000001' WHERE "TenantId" IS NULL;
            ALTER TABLE "Invoices" ALTER COLUMN "TenantId" SET NOT NULL;
            ALTER TABLE "Invoices" DROP CONSTRAINT IF EXISTS "FK_Invoices_Tenants_TenantId";
            ALTER TABLE "Invoices" ADD CONSTRAINT "FK_Invoices_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            CREATE INDEX IF NOT EXISTS "IX_Invoices_TenantId" ON "Invoices" ("TenantId");
            CREATE INDEX IF NOT EXISTS "IX_Invoices_TenantId_InvoiceNumber" ON "Invoices" ("TenantId", "InvoiceNumber");

            ALTER TABLE "Discrepancies" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
            UPDATE "Discrepancies" d
            SET "TenantId" = COALESCE(
                (SELECT o."TenantId" FROM "Orders" o WHERE o."Id" = d."OrderId"),
                (SELECT p."TenantId" FROM "Payments" p WHERE p."Id" = d."PaymentId"),
                (SELECT i."TenantId" FROM "Invoices" i WHERE i."Id" = d."InvoiceId")
            )
            WHERE d."TenantId" IS NULL;
            UPDATE "Discrepancies" SET "TenantId" = '00000000-0000-0000-0000-000000000001' WHERE "TenantId" IS NULL;
            ALTER TABLE "Discrepancies" ALTER COLUMN "TenantId" SET NOT NULL;
            ALTER TABLE "Discrepancies" DROP CONSTRAINT IF EXISTS "FK_Discrepancies_Tenants_TenantId";
            ALTER TABLE "Discrepancies" ADD CONSTRAINT "FK_Discrepancies_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            CREATE INDEX IF NOT EXISTS "IX_Discrepancies_TenantId" ON "Discrepancies" ("TenantId");
            CREATE INDEX IF NOT EXISTS "IX_Discrepancies_TenantId_Type_Status" ON "Discrepancies" ("TenantId", "Type", "Status");

            ALTER TABLE "FinancialCases" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
            UPDATE "FinancialCases" fc
            SET "TenantId" = d."TenantId"
            FROM "Discrepancies" d
            WHERE fc."DiscrepancyId" = d."Id" AND fc."TenantId" IS NULL;
            UPDATE "FinancialCases" SET "TenantId" = '00000000-0000-0000-0000-000000000001' WHERE "TenantId" IS NULL;
            ALTER TABLE "FinancialCases" ALTER COLUMN "TenantId" SET NOT NULL;
            ALTER TABLE "FinancialCases" DROP CONSTRAINT IF EXISTS "FK_FinancialCases_Tenants_TenantId";
            ALTER TABLE "FinancialCases" ADD CONSTRAINT "FK_FinancialCases_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            CREATE INDEX IF NOT EXISTS "IX_FinancialCases_TenantId" ON "FinancialCases" ("TenantId");

            ALTER TABLE "FinancialCaseLinks" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
            UPDATE "FinancialCaseLinks" l
            SET "TenantId" = fc."TenantId"
            FROM "FinancialCases" fc
            WHERE l."FinancialCaseId" = fc."Id" AND l."TenantId" IS NULL;
            UPDATE "FinancialCaseLinks" SET "TenantId" = '00000000-0000-0000-0000-000000000001' WHERE "TenantId" IS NULL;
            ALTER TABLE "FinancialCaseLinks" ALTER COLUMN "TenantId" SET NOT NULL;
            ALTER TABLE "FinancialCaseLinks" DROP CONSTRAINT IF EXISTS "FK_FinancialCaseLinks_Tenants_TenantId";
            ALTER TABLE "FinancialCaseLinks" ADD CONSTRAINT "FK_FinancialCaseLinks_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            CREATE INDEX IF NOT EXISTS "IX_FinancialCaseLinks_TenantId" ON "FinancialCaseLinks" ("TenantId");

            ALTER TABLE "CaseAuditEvents" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
            UPDATE "CaseAuditEvents" a
            SET "TenantId" = fc."TenantId"
            FROM "FinancialCases" fc
            WHERE a."FinancialCaseId" = fc."Id" AND a."TenantId" IS NULL;
            UPDATE "CaseAuditEvents" SET "TenantId" = '00000000-0000-0000-0000-000000000001' WHERE "TenantId" IS NULL;
            ALTER TABLE "CaseAuditEvents" ALTER COLUMN "TenantId" SET NOT NULL;
            ALTER TABLE "CaseAuditEvents" DROP CONSTRAINT IF EXISTS "FK_CaseAuditEvents_Tenants_TenantId";
            ALTER TABLE "CaseAuditEvents" ADD CONSTRAINT "FK_CaseAuditEvents_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            CREATE INDEX IF NOT EXISTS "IX_CaseAuditEvents_TenantId" ON "CaseAuditEvents" ("TenantId");

            ALTER TABLE "FinancialGraphRelations" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
            UPDATE "FinancialGraphRelations" g
            SET "TenantId" = COALESCE(
                (SELECT o."TenantId" FROM "Orders" o WHERE g."FromEntityId" = o."Id" OR g."ToEntityId" = o."Id"),
                (SELECT p."TenantId" FROM "Payments" p WHERE g."FromEntityId" = p."Id" OR g."ToEntityId" = p."Id"),
                (SELECT d."TenantId" FROM "Discrepancies" d WHERE g."FromEntityId" = d."Id" OR g."ToEntityId" = d."Id"),
                (SELECT fc."TenantId" FROM "FinancialCases" fc WHERE g."FromEntityId" = fc."Id" OR g."ToEntityId" = fc."Id"),
                (SELECT l."TenantId" FROM "FinancialCaseLinks" l WHERE g."FromEntityId" = l."Id" OR g."ToEntityId" = l."Id")
            )
            WHERE g."TenantId" IS NULL;
            UPDATE "FinancialGraphRelations" SET "TenantId" = '00000000-0000-0000-0000-000000000001' WHERE "TenantId" IS NULL;
            ALTER TABLE "FinancialGraphRelations" ALTER COLUMN "TenantId" SET NOT NULL;
            ALTER TABLE "FinancialGraphRelations" DROP CONSTRAINT IF EXISTS "FK_FinancialGraphRelations_Tenants_TenantId";
            ALTER TABLE "FinancialGraphRelations" ADD CONSTRAINT "FK_FinancialGraphRelations_Tenants_TenantId"
                FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;
            DROP INDEX IF EXISTS "IX_FinancialGraphRelations_Nodes_Relation";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_FinancialGraphRelations_TenantId_Nodes_Relation"
                ON "FinancialGraphRelations" ("TenantId", "FromNodeType", "FromEntityId", "ToNodeType", "ToEntityId", "RelationType");
            CREATE INDEX IF NOT EXISTS "IX_FinancialGraphRelations_TenantId" ON "FinancialGraphRelations" ("TenantId");


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

            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "ShopCurrency" varchar(3) NOT NULL DEFAULT 'EUR';

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

            ALTER TABLE "IntegrationConnections" ALTER COLUMN "ApiKeyHash" DROP NOT NULL;
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "ShopDomain" varchar(255) NULL;
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "EncryptedAccessToken" text NULL;
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "EncryptedRefreshToken" text NULL;
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "AccessTokenExpiresAtUtc" timestamp with time zone NULL;
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "RefreshTokenExpiresAtUtc" timestamp with time zone NULL;
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "Scopes" varchar(1000) NOT NULL DEFAULT '';
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "LastSyncAtUtc" timestamp with time zone NULL;
            ALTER TABLE "IntegrationConnections" ADD COLUMN IF NOT EXISTS "LastWebhookAtUtc" timestamp with time zone NULL;
            CREATE INDEX IF NOT EXISTS "IX_IntegrationConnections_TenantId_Provider_ShopDomain"
                ON "IntegrationConnections" ("TenantId", "Provider", "ShopDomain");

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
