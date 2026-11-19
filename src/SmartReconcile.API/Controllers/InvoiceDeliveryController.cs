using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Services;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/settings/invoices")]
public sealed class InvoiceSettingsController(SmartReconcileDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<InvoiceSettingsDto>> Get(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();

        var settings = await db.AccountingSettings.FirstOrDefaultAsync(x => x.TenantId == tenant.Id, ct);
        var connection = await db.IntegrationConnections.FirstOrDefaultAsync(
            x => x.TenantId == tenant.Id && x.Provider == IntegrationProvider.Shopify,
            ct);

        if (settings is null)
        {
            settings = AccountingSettings.CreateDefault(tenant.Id);
            db.AccountingSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }

        return Ok(new InvoiceSettingsDto(
            settings.AutomaticInvoiceCreationEnabled,
            settings.CompanyName,
            settings.CompanyAddress,
            settings.TaxNumber,
            settings.VatId,
            settings.BankDetails,
            connection?.InvoiceAccessTokenLast4,
            connection?.InvoiceAccessTokenUpdatedAtUtc));
    }

    [HttpPut]
    public async Task<ActionResult<InvoiceSettingsDto>> Update(UpdateInvoiceSettingsRequest request, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();

        var settings = await db.AccountingSettings.FirstOrDefaultAsync(x => x.TenantId == tenant.Id, ct);
        if (settings is null)
        {
            settings = AccountingSettings.CreateDefault(tenant.Id);
            db.AccountingSettings.Add(settings);
        }

        settings.UpdateInvoiceSettings(
            request.AutomaticInvoiceCreationEnabled,
            request.CompanyName,
            request.CompanyAddress,
            request.TaxNumber,
            request.VatId,
            request.BankDetails);

        await db.SaveChangesAsync(ct);

        var connection = await db.IntegrationConnections.FirstOrDefaultAsync(
            x => x.TenantId == tenant.Id && x.Provider == IntegrationProvider.Shopify,
            ct);

        return Ok(new InvoiceSettingsDto(
            settings.AutomaticInvoiceCreationEnabled,
            settings.CompanyName,
            settings.CompanyAddress,
            settings.TaxNumber,
            settings.VatId,
            settings.BankDetails,
            connection?.InvoiceAccessTokenLast4,
            connection?.InvoiceAccessTokenUpdatedAtUtc));
    }

    [HttpPost("access-token/rotate")]
    public async Task<ActionResult<InvoiceAccessTokenDto>> RotateToken(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        var connection = ShopifyAuthenticationContext.GetConnection(HttpContext);
        if (tenant is null || connection is null || connection.Provider != IntegrationProvider.Shopify)
            return BadRequest(new { message = "Keine aktive Shopify-Verbindung gefunden." });

        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        connection.SetInvoiceAccessTokenHash(hash, token[^4..]);
        await db.SaveChangesAsync(ct);

        var shop = connection.ShopDomain ?? connection.ExternalStoreId;
        var snippet = $"<p><a href=\"https://smartreconcile-ai-api.onrender.com/public/invoices/shopify/{{{{ order.id }}}}.pdf?shop={shop}&token={token}\">Rechnung als PDF herunterladen</a></p>";

        return Ok(new InvoiceAccessTokenDto(token, token[^4..], connection.InvoiceAccessTokenUpdatedAtUtc!.Value, snippet));
    }
}

[ApiController]
public sealed class PublicInvoiceController(
    SmartReconcileDbContext db,
    InvoiceAutomationService automation) : ControllerBase
{
    [HttpGet("/public/invoices/shopify/{shopifyOrderId}.pdf")]
    public async Task<IActionResult> GetShopifyInvoice(
        string shopifyOrderId,
        [FromQuery] string shop,
        [FromQuery] string token,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(shopifyOrderId) ||
            string.IsNullOrWhiteSpace(shop) ||
            string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var normalizedShop = shop.Trim().ToLowerInvariant();
        var connection = await db.IntegrationConnections.FirstOrDefaultAsync(
            x => x.Provider == IntegrationProvider.Shopify &&
                 x.ShopDomain == normalizedShop &&
                 x.Status != ConnectionStatus.Revoked,
            ct);

        if (connection?.InvoiceAccessTokenHash is null)
            return NotFound();

        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var expectedHash = Convert.FromHexString(connection.InvoiceAccessTokenHash);
        if (!CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash))
            return NotFound();

        var order = await db.Orders.FirstOrDefaultAsync(
            x => x.TenantId == connection.TenantId && x.ShopifyOrderId == shopifyOrderId,
            ct);

        if (order is null)
            return NotFound();

        var invoice = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == connection.TenantId &&
                 x.OrderId == order.Id &&
                 x.DocumentType == InvoiceDocumentType.Invoice,
            ct);

        if (invoice is null && order.Status == OrderStatus.Paid)
            invoice = await automation.EnsureSalesInvoiceAsync(connection.TenantId, shopifyOrderId, order, ct);

        if (invoice?.PdfContent is null)
            return NotFound();

        var fileName = $"Rechnung-{SanitizeFileName(invoice.InvoiceNumber)}.pdf";
        return File(invoice.PdfContent, "application/pdf", fileName);
    }

    private static string SanitizeFileName(string value)
        => string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_'));
}
