using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class ControlTowerService(
    IFinancialCaseRepository cases,
    IFinancialCaseLinkRepository caseLinks,
    ICaseAuditEventRepository auditEvents,
    IFinancialGraphRepository graph,
    IDiscrepancyRepository discrepancies,
    IOrderRepository orders,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IIntegrationConnectionRepository connections,
    IAiAnalysisService aiAnalysis) : IControlTowerService
{
    public async Task<ControlTowerDto> GetOverviewAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await EnsureFinancialCasesAsync(tenantId, cancellationToken);

        var orderList = await orders.GetAllAsync(tenantId, cancellationToken);
        var paymentList = await payments.GetAllAsync(tenantId, cancellationToken);
        var caseList = await cases.GetAllAsync(tenantId, cancellationToken);
        var connectionList = await connections.GetForTenantAsync(tenantId, cancellationToken);
        var shopCurrency = connectionList.FirstOrDefault(x => x.Provider == IntegrationProvider.Shopify)?.ShopCurrency ?? "EUR";

        var revenue = decimal.Round(orderList.Where(x => string.Equals(x.Currency, shopCurrency, StringComparison.OrdinalIgnoreCase)).Sum(x => x.TotalAmount), 2);
        var collected = decimal.Round(
            paymentList.Where(x => string.Equals(x.Currency, shopCurrency, StringComparison.OrdinalIgnoreCase) && x.Status == PaymentStatus.Succeeded && (x.Kind is PaymentKind.Sale or PaymentKind.Capture)).Sum(x => x.Amount)
            - paymentList.Where(x => string.Equals(x.Currency, shopCurrency, StringComparison.OrdinalIgnoreCase) && x.Status == PaymentStatus.Refunded && x.Kind == PaymentKind.Refund).Sum(x => x.Amount), 2);
        var cashGap = decimal.Round(Math.Max(0, revenue - collected), 2);
        var openCases = caseList.Where(x => x.Status is FinancialCaseStatus.Open or FinancialCaseStatus.Investigating).ToList();
        var affectedAmount = decimal.Round(openCases.Sum(x => x.AffectedAmount), 2);
        var highRisk = openCases.Count(x => x.Severity is FinancialCaseSeverity.High or FinancialCaseSeverity.Critical);
        var orderCases = openCases.Where(x => x.DiscrepancyId.HasValue).ToList();
        var affectedOrderIds = new HashSet<Guid>();
        foreach (var discrepancyCase in orderCases)
        {
            var discrepancy = await discrepancies.GetByIdAsync(discrepancyCase.DiscrepancyId!.Value, tenantId, cancellationToken);
            if (discrepancy?.OrderId is Guid orderId) affectedOrderIds.Add(orderId);
        }
        var orderCount = orderList.Count;
        var matchedOrderCount = Math.Max(0, orderCount - affectedOrderIds.Count);
        var score = CalculateIntegrityScore(orderCount, matchedOrderCount);
        var explanation = BuildIntegrityExplanation(orderCount, matchedOrderCount, affectedOrderIds.Count, openCases.Count);

        var health = connectionList
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.DisplayName)
            .Select(MapHealth)
            .ToList();

        var caseDtos = new List<FinancialCaseDto>();
        foreach (var item in openCases.OrderByDescending(x => x.Severity).ThenByDescending(x => x.DetectedAtUtc).Take(20))
            caseDtos.Add(await MapCaseAsync(tenantId, item, cancellationToken));

        return new ControlTowerDto(
            score,
            explanation,
            revenue,
            revenue,
            collected,
            cashGap,
            openCases.Count,
            highRisk,
            affectedAmount,
            health,
            caseDtos);
    }

    public async Task<FinancialCaseDto?> GetCaseAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, tenantId, cancellationToken);
        return item is null ? null : await MapCaseAsync(tenantId, item, cancellationToken);
    }

    public async Task<FinancialCaseDetailDto?> GetCaseDetailAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, tenantId, cancellationToken);
        if (item is null)
            return null;

        var links = await caseLinks.GetForCaseAsync(tenantId, item.Id, cancellationToken);
        var timeline = await auditEvents.GetForCaseAsync(tenantId, item.Id, cancellationToken);
        var discrepancy = item.DiscrepancyId.HasValue
            ? await discrepancies.GetByIdAsync(item.DiscrepancyId.Value, tenantId, cancellationToken)
            : null;

        var graphNodes = await BuildGraphNodesAsync(tenantId, discrepancy, links, cancellationToken);

        var analysis = discrepancy?.AiAnalysis ?? item.RootCause;
        var recommendedAction = timeline
            .Where(x => x.EventType == CaseAuditEventType.Investigated)
            .Select(x => ExtractRecommendedAction(x.Description))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var rootCauseAnalysis = new RootCauseAnalysisDto(
            string.IsNullOrWhiteSpace(analysis) ? null : analysis,
            recommendedAction,
            !string.IsNullOrWhiteSpace(analysis),
            timeline.Any(x => x.EventType == CaseAuditEventType.Investigated && x.Description.Contains("Fallback", StringComparison.OrdinalIgnoreCase)));

        return new FinancialCaseDetailDto(
            item.Id,
            item.CaseNumber ?? BuildCaseNumber(item),
            item.DiscrepancyId,
            item.Title,
            item.Description,
            item.Severity,
            item.Status,
            item.AffectedAmount,
            item.Currency,
            item.DetectedAtUtc,
            item.ResolvedAtUtc,
            timeline.Select(x => new CaseAuditEventDto(
                x.Id,
                x.EventType,
                x.Description,
                x.Actor,
                x.CreatedAtUtc)).ToList(),
            graphNodes,
            rootCauseAnalysis);
    }

    public async Task<FinancialCaseDto?> StartInvestigationAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, tenantId, cancellationToken);
        if (item is null)
            return null;

        var links = await caseLinks.GetForCaseAsync(tenantId, item.Id, cancellationToken);
        var discrepancy = item.DiscrepancyId.HasValue
            ? await discrepancies.GetByIdAsync(item.DiscrepancyId.Value, tenantId, cancellationToken)
            : null;

        item.StartInvestigation();

        var context = await BuildAiContextAsync(tenantId, discrepancy, links, cancellationToken);
        var aiResult = await aiAnalysis.AnalyzeFinancialCaseAsync(
            new FinancialCaseAnalysisRequest(item.Title, item.Description, context),
            cancellationToken);

        item.SetRootCause(aiResult.Analysis);

        if (discrepancy is not null)
            discrepancy.SetAiAnalysis(aiResult.Analysis);

        await auditEvents.AddAsync(
            CaseAuditEvent.Create(
                tenantId,
                item.Id,
                CaseAuditEventType.Investigated,
                $"KI-Ursachenanalyse durchgeführt{(aiResult.IsFallback ? " (Fallback)" : string.Empty)}. Ursache: {aiResult.Analysis} Empfehlung: {aiResult.RecommendedAction}",
                "AI Engine"),
            cancellationToken);

        await cases.SaveAsync(cancellationToken);
        return await MapCaseAsync(tenantId, item, cancellationToken);
    }

    public async Task<FinancialCaseDto?> AutoFixAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, tenantId, cancellationToken);
        if (item is null)
            return null;

        var discrepancy = item.DiscrepancyId.HasValue
            ? await discrepancies.GetByIdAsync(item.DiscrepancyId.Value, tenantId, cancellationToken)
            : null;

        var description = "Automatische Korrekturmaßnahme wurde ausgelöst.";
        var actionWasApplied = false;

        if (discrepancy?.Type == DiscrepancyType.OrphanedPayment &&
            discrepancy.OrderId.HasValue &&
            discrepancy.PaymentId.HasValue)
        {
            var payment = await payments.GetByIdAsync(discrepancy.PaymentId.Value, tenantId, cancellationToken);
            var order = await orders.GetByIdAsync(discrepancy.OrderId.Value, tenantId, cancellationToken);

            if (payment?.Status == PaymentStatus.Succeeded && order is not null)
            {
                order.ChangeStatus(OrderStatus.Paid);
                actionWasApplied = true;
                description = $"Bestellung {order.OrderNumber} wurde nach erfolgreicher Zahlung automatisch auf 'Bezahlt' gesetzt.";
            }
        }

        if (!actionWasApplied)
        {
            description = "Automatische Korrekturprüfung ausgeführt. Für diesen Fall ist keine sichere automatische Buchungskorrektur hinterlegt; eine manuelle Prüfung bleibt erforderlich.";
        }

        await auditEvents.AddAsync(
            CaseAuditEvent.Create(
                tenantId,
                item.Id,
                CaseAuditEventType.ActionExecuted,
                description,
                "System"),
            cancellationToken);

        await cases.SaveAsync(cancellationToken);
        return await MapCaseAsync(tenantId, item, cancellationToken);
    }

    public async Task<FinancialCaseDto?> ResolveAsync(
        Guid tenantId,
        Guid id,
        string? rootCause,
        string? note,
        CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, tenantId, cancellationToken);
        if (item is null)
            return null;

        item.Resolve(rootCause);

        var description = string.IsNullOrWhiteSpace(note)
            ? "Finanzfall wurde als gelöst markiert."
            : $"Finanzfall wurde als gelöst markiert. Notiz: {note.Trim()}";

        await auditEvents.AddAsync(
            CaseAuditEvent.Create(
                tenantId,
                item.Id,
                CaseAuditEventType.Resolved,
                description,
                "User"),
            cancellationToken);

        await cases.SaveAsync(cancellationToken);
        return await MapCaseAsync(tenantId, item, cancellationToken);
    }

    public async Task<FinancialCaseDto?> IgnoreAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, tenantId, cancellationToken);
        if (item is null)
            return null;

        item.Ignore();

        await auditEvents.AddAsync(
            CaseAuditEvent.Create(
                tenantId,
                item.Id,
                CaseAuditEventType.Ignored,
                "Finanzfall wurde vom Benutzer ignoriert.",
                "User"),
            cancellationToken);

        await cases.SaveAsync(cancellationToken);
        return await MapCaseAsync(tenantId, item, cancellationToken);
    }

    public async Task<List<FinancialGraphRelationDto>> GetGraphAsync(
        Guid tenantId,
        FinancialNodeType nodeType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var relations = await graph.GetForNodeAsync(tenantId, nodeType, entityId, cancellationToken);
        return relations.Select(x => new FinancialGraphRelationDto(
            x.Id,
            x.FromNodeType,
            x.FromEntityId,
            x.ToNodeType,
            x.ToEntityId,
            x.RelationType)).ToList();
    }

    private async Task EnsureFinancialCasesAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var discrepancyList = await discrepancies.GetAllAsync(tenantId, cancellationToken);

        foreach (var discrepancy in discrepancyList)
        {
            if (discrepancy.Status is DiscrepancyStatus.Ignored or DiscrepancyStatus.Resolved)
                continue;

            if (await cases.ExistsOpenForDiscrepancyAsync(tenantId, discrepancy.Id, cancellationToken))
                continue;

            var financialCase = FinancialCase.Create(
                tenantId,
                discrepancy.Id,
                DetermineSeverity(discrepancy),
                discrepancy.Title,
                discrepancy.Description,
                discrepancy.AffectedAmount);

            await cases.AddAsync(financialCase, cancellationToken);

            var links = new List<FinancialCaseLink>
            {
                FinancialCaseLink.Create(
                    tenantId,
                    financialCase.Id,
                    FinancialNodeType.Discrepancy,
                    discrepancy.Id,
                    "Abweichung",
                    discrepancy.Title,
                    discrepancy.AffectedAmount)
            };

            if (discrepancy.OrderId.HasValue)
            {
                var order = await orders.GetByIdAsync(discrepancy.OrderId.Value, tenantId, cancellationToken);
                if (order is not null)
                    links.Add(FinancialCaseLink.Create(
                        tenantId,
                        financialCase.Id,
                        FinancialNodeType.Order,
                        order.Id,
                        "Bestellung",
                        order.OrderNumber,
                        order.TotalAmount));
            }

            if (discrepancy.PaymentId.HasValue)
            {
                var payment = await payments.GetByIdAsync(discrepancy.PaymentId.Value, tenantId, cancellationToken);
                if (payment is not null)
                    links.Add(FinancialCaseLink.Create(
                        tenantId,
                        financialCase.Id,
                        FinancialNodeType.Payment,
                        payment.Id,
                        "Zahlung",
                        payment.TransactionId,
                        payment.Amount));
            }

            if (discrepancy.InvoiceId.HasValue)
            {
                var invoice = await invoices.GetByIdAsync(discrepancy.InvoiceId.Value, tenantId, cancellationToken);
                if (invoice is not null)
                    links.Add(FinancialCaseLink.Create(
                        tenantId,
                        financialCase.Id,
                        FinancialNodeType.Invoice,
                        invoice.Id,
                        "Rechnung",
                        invoice.InvoiceNumber,
                        invoice.Amount));
            }

            await caseLinks.AddRangeAsync(links, cancellationToken);

            await auditEvents.AddAsync(
                CaseAuditEvent.Create(
                    tenantId,
                    financialCase.Id,
                    CaseAuditEventType.Detected,
                    $"Finanzfall erkannt: {financialCase.Title}. Betroffener Betrag: {financialCase.AffectedAmount:0.00} EUR.",
                    "System"),
                cancellationToken);
        }

        await cases.SaveAsync(cancellationToken);

        var orderList = await orders.GetAllAsync(tenantId, cancellationToken);
        var paymentList = await payments.GetAllAsync(tenantId, cancellationToken);

        foreach (var payment in paymentList)
        {
            var order = orderList.FirstOrDefault(x => x.Id == payment.OrderId);
            if (order is null) continue;

            if (!await graph.ExistsAsync(
                tenantId,
                FinancialNodeType.Order, order.Id,
                FinancialNodeType.Payment, payment.Id,
                FinancialRelationType.PaymentFor,
                cancellationToken))
            {
                await graph.AddAsync(
                    FinancialGraphRelation.Create(
                        tenantId,
                        FinancialNodeType.Order,
                        order.Id,
                        FinancialNodeType.Payment,
                        payment.Id,
                        FinancialRelationType.PaymentFor),
                    cancellationToken);
            }
        }

        await graph.SaveAsync(cancellationToken);
    }

    private async Task<List<FinancialGraphNodeDto>> BuildGraphNodesAsync(
        Guid tenantId,
        Discrepancy? discrepancy,
        IReadOnlyList<FinancialCaseLink> links,
        CancellationToken cancellationToken)
    {
        var orderId = discrepancy?.OrderId ?? links.FirstOrDefault(x => x.NodeType == FinancialNodeType.Order)?.EntityId;
        var paymentId = discrepancy?.PaymentId ?? links.FirstOrDefault(x => x.NodeType == FinancialNodeType.Payment)?.EntityId;
        var invoiceId = discrepancy?.InvoiceId ?? links.FirstOrDefault(x => x.NodeType == FinancialNodeType.Invoice)?.EntityId;

        var order = orderId.HasValue
            ? await orders.GetByIdAsync(orderId.Value, tenantId, cancellationToken)
            : null;

        // Legacy discrepancies created before OrderId/PaymentId were persisted may still
        // have null references. Recover the real order from the discrepancy description so
        // older financial cases do not render an entirely empty document chain.
        if (order is null && discrepancy is not null)
        {
            var allOrders = await orders.GetAllAsync(tenantId, cancellationToken);
            order = allOrders.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(x.OrderNumber) && discrepancy.Description.Contains(x.OrderNumber, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(x.OrderNumber) && discrepancy.Title.Contains(x.OrderNumber, StringComparison.OrdinalIgnoreCase)));

            orderId = order?.Id;
        }

        // A discrepancy may not carry PaymentId when there is no payment (MissingPayment)
        // or when the rule is order-level (e.g. CurrencyMismatch). In that case resolve
        // the first real payment for the affected order instead of showing a false "Fehlt".
        var payment = paymentId.HasValue
            ? await payments.GetByIdAsync(paymentId.Value, tenantId, cancellationToken)
            : orderId.HasValue
                ? await payments.GetForOrderAsync(orderId.Value, tenantId, cancellationToken)
                : null;

        var invoice = invoiceId.HasValue ? await invoices.GetByIdAsync(invoiceId.Value, tenantId, cancellationToken) : null;

        return
        [
            order is null
                ? MissingNode(FinancialNodeType.Order)
                : new FinancialGraphNodeDto(
                    FinancialNodeType.Order,
                    order.Id,
                    true,
                    MapOrderStatus(order.Status),
                    order.OrderNumber,
                    order.TotalAmount,
                    order.Currency,
                    order.CreatedAtUtc,
                    $"Kunde: {order.CustomerName}"),
            payment is null
                ? PaymentMissingNode()
                : new FinancialGraphNodeDto(
                    FinancialNodeType.Payment,
                    payment.Id,
                    true,
                    MapPaymentStatus(payment.Status),
                    payment.TransactionId,
                    payment.Amount,
                    payment.Currency,
                    payment.PaidAtUtc,
                    $"Zahlungstransaktion{(string.IsNullOrWhiteSpace(payment.Gateway) ? string.Empty : $" · {payment.Gateway}")}"),
            invoice is null
                ? InvoiceNotLinkedNode()
                : new FinancialGraphNodeDto(
                    FinancialNodeType.Invoice,
                    invoice.Id,
                    true,
                    "Vorhanden",
                    invoice.InvoiceNumber,
                    invoice.Amount,
                    invoice.Currency,
                    invoice.InvoiceDateUtc,
                    $"Lieferant: {invoice.SupplierName}")
        ];
    }

    private async Task<string> BuildAiContextAsync(
        Guid tenantId,
        Discrepancy? discrepancy,
        IReadOnlyList<FinancialCaseLink> links,
        CancellationToken cancellationToken)
    {
        var nodes = await BuildGraphNodesAsync(tenantId, discrepancy, links, cancellationToken);
        return string.Join(
            " | ",
            nodes.Select(x =>
                $"{x.NodeType}: Status={x.Status}, Kennung={x.Identifier ?? "Fehlt"}, Betrag={(x.Amount?.ToString("0.00") ?? "Fehlt")} {x.Currency}"));
    }

    private static string ExtractRecommendedAction(string description)
    {
        const string marker = "Empfehlung:";
        var index = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? string.Empty : description[(index + marker.Length)..].Trim();
    }

    private static FinancialGraphNodeDto MissingNode(FinancialNodeType type)
        => new(type, null, false, "Fehlt", null, null, "EUR", null, "Keine verknüpfte Entität vorhanden.");

    private static FinancialGraphNodeDto PaymentMissingNode()
        => new(
            FinancialNodeType.Payment,
            null,
            false,
            "Kein Zahlungseingang",
            null,
            null,
            "EUR",
            null,
            "Für diese Bestellung wurde keine passende Zahlungstransaktion verbucht.");

    private static FinancialGraphNodeDto InvoiceNotLinkedNode()
        => new(
            FinancialNodeType.Invoice,
            null,
            false,
            "Nicht verknüpft",
            null,
            null,
            "EUR",
            null,
            "Nicht verknüpft (Shopify-Direktabgleich).");

    private static string MapOrderStatus(OrderStatus status)
        => status switch
        {
            OrderStatus.Pending => "Offen",
            OrderStatus.Paid => "Bezahlt",
            OrderStatus.OnHold => "Zurückgestellt",
            OrderStatus.Cancelled => "Storniert",
            OrderStatus.Refunded => "Erstattet",
            _ => "Unbekannt"
        };

    private static string MapPaymentStatus(PaymentStatus status)
        => status switch
        {
            PaymentStatus.Pending => "Ausstehend",
            PaymentStatus.Succeeded => "Erfolgreich",
            PaymentStatus.Failed => "Fehlgeschlagen",
            PaymentStatus.Refunded => "Erstattet",
            _ => "Unbekannt"
        };

    private async Task<FinancialCaseDto> MapCaseAsync(Guid tenantId, FinancialCase item, CancellationToken cancellationToken)
    {
        var links = await caseLinks.GetForCaseAsync(tenantId, item.Id, cancellationToken);
        return new FinancialCaseDto(
            item.Id,
            item.DiscrepancyId,
            item.Status,
            item.Severity,
            item.Title,
            item.Description,
            item.RootCause,
            item.AffectedAmount,
            item.Currency,
            item.DetectedAtUtc,
            item.ResolvedAtUtc,
            links.Select(x => new FinancialCaseLinkDto(
                x.NodeType,
                x.EntityId,
                x.Role,
                x.Label,
                x.Amount,
                x.Currency)).ToList());
    }

    private static string BuildCaseNumber(FinancialCase item)
        => $"CASE-{item.DetectedAtUtc:yyyy}-{item.Id.ToString("N")[..6].ToUpperInvariant()}";

    private static FinancialCaseSeverity DetermineSeverity(Discrepancy discrepancy)
    {
        if (discrepancy.Type == DiscrepancyType.OrphanedPayment)
            return FinancialCaseSeverity.High;

        if (discrepancy.Type == DiscrepancyType.MissingPayment)
            return discrepancy.AffectedAmount >= 500 ? FinancialCaseSeverity.High : FinancialCaseSeverity.Medium;

        if (discrepancy.Type == DiscrepancyType.AmountMismatch)
            return discrepancy.AffectedAmount >= 100 ? FinancialCaseSeverity.High : FinancialCaseSeverity.Medium;

        return discrepancy.AffectedAmount >= 500 ? FinancialCaseSeverity.High : FinancialCaseSeverity.Medium;
    }

    private static decimal CalculateIntegrityScore(int orderCount, int matchedOrderCount)
    {
        if (orderCount <= 0) return 100m;
        return decimal.Round(Math.Clamp(matchedOrderCount * 100m / orderCount, 0m, 100m), 1);
    }

    private static string BuildIntegrityExplanation(int orderCount, int matchedOrderCount, int affectedOrderCount, int openCaseCount)
    {
        if (orderCount == 0)
            return "Noch keine Bestellungen für die finanzielle Integritätsprüfung vorhanden.";

        if (affectedOrderCount == 0 && openCaseCount == 0)
            return $"Alle {orderCount} Bestellungen stimmen mit den verbuchten Zahlungseingängen überein. Keine offenen Abweichungen.";

        return $"{matchedOrderCount} von {orderCount} Bestellungen sind zahlungsseitig abgestimmt. {affectedOrderCount} Bestellungen sind von offenen Abweichungen betroffen; insgesamt {openCaseCount} offene Finanzfälle.";
    }

    private static SystemHealthDto MapHealth(Domain.Entities.IntegrationConnection connection)
    {
        if (connection.Status is ConnectionStatus.Revoked or ConnectionStatus.Disconnected)
            return new SystemHealthDto(
                connection.Id,
                connection.DisplayName,
                connection.Provider.ToString(),
                "Disconnected",
                "Verbindung ist nicht aktiv.",
                connection.LastWebhookAtUtc ?? connection.LastSeenAtUtc);

        if (connection.Status == ConnectionStatus.Warning)
            return new SystemHealthDto(
                connection.Id,
                connection.DisplayName,
                connection.Provider.ToString(),
                "Warning",
                "Verbindung benötigt Aufmerksamkeit.",
                connection.LastWebhookAtUtc ?? connection.LastSeenAtUtc);

        var lastActivity = connection.Provider == IntegrationProvider.Shopify
            ? connection.LastWebhookAtUtc
            : connection.LastSeenAtUtc;

        if (lastActivity is null)
            return new SystemHealthDto(
                connection.Id,
                connection.DisplayName,
                connection.Provider.ToString(),
                "Warning",
                "Noch keine Aktivität empfangen.",
                null);

        var age = DateTime.UtcNow - lastActivity.Value;
        if (age <= TimeSpan.FromHours(24))
            return new SystemHealthDto(
                connection.Id,
                connection.DisplayName,
                connection.Provider.ToString(),
                "Healthy",
                null,
                lastActivity);

        return new SystemHealthDto(
            connection.Id,
            connection.DisplayName,
            connection.Provider.ToString(),
            "Warning",
            "Seit mehr als 24 Stunden keine Aktivität empfangen.",
            lastActivity);
    }
}
