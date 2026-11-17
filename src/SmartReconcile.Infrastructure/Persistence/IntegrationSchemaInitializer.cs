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
