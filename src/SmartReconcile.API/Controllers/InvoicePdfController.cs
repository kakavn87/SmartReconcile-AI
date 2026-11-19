using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartReconcile.API.Authentication;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Services;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicePdfController(
    SmartReconcileDbContext db,
    InvoicePdfService pdf) : ControllerBase
{
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();

        var invoice = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenant.Id && x.Id == id,
            ct);
        if (invoice is null) return NotFound();

        if (invoice.PdfContent is null)
        {
            var settings = await db.AccountingSettings.FirstOrDefaultAsync(x => x.TenantId == tenant.Id, ct);
            invoice.AttachPdf(pdf.Generate(invoice, settings ?? SmartReconcile.Domain.Entities.AccountingSettings.CreateDefault(tenant.Id)));
            await db.SaveChangesAsync(ct);
        }

        var fileName = $"Rechnung-{string.Concat(invoice.InvoiceNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_'))}.pdf";
        return File(invoice.PdfContent!, "application/pdf", fileName);
    }
}
