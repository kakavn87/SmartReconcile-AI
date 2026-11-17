using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class ControlTowerService(
    IFinancialCaseRepository cases,
    IFinancialCaseLinkRepository caseLinks,
    IFinancialGraphRepository graph,
    IDiscrepancyRepository discrepancies,
    IOrderRepository orders,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IIntegrationConnectionRepository connections) : IControlTowerService
{
    public async Task<ControlTowerDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        await EnsureFinancialCasesAsync(cancellationToken);

        var orderList = await orders.GetAllAsync(cancellationToken);
        var paymentList = await payments.GetAllAsync(cancellationToken);
        var caseList = await cases.GetAllAsync(cancellationToken);
        var connectionList = await connections.GetAllAsync(cancellationToken);

        var revenue = decimal.Round(orderList.Sum(x => x.TotalAmount), 2);
        var collected = decimal.Round(paymentList
            .Where(x => x.Status == PaymentStatus.Succeeded)
            .Sum(x => x.Amount), 2);
        var cashGap = decimal.Round(Math.Max(0, revenue - collected), 2);
        var openCases = caseList.Where(x => x.Status is FinancialCaseStatus.Open or FinancialCaseStatus.Investigating).ToList();
        var affectedAmount = decimal.Round(openCases.Sum(x => x.AffectedAmount), 2);
        var highRisk = openCases.Count(x => x.Severity is FinancialCaseSeverity.High or FinancialCaseSeverity.Critical);
        var score = CalculateIntegrityScore(revenue, collected, affectedAmount, openCases);

        var health = connectionList
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.DisplayName)
            .Select(MapHealth)
            .ToList();

        var caseDtos = new List<FinancialCaseDto>();
        foreach (var item in openCases.OrderByDescending(x => x.Severity).ThenByDescending(x => x.DetectedAtUtc).Take(20))
            caseDtos.Add(await MapCaseAsync(item, cancellationToken));

        return new ControlTowerDto(
            score,
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

    public async Task<FinancialCaseDto?> GetCaseAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, cancellationToken);
        return item is null ? null : await MapCaseAsync(item, cancellationToken);
    }

    public async Task<FinancialCaseDto?> StartInvestigationAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, cancellationToken);
        if (item is null) return null;
        item.StartInvestigation();
        await cases.SaveAsync(cancellationToken);
        return await MapCaseAsync(item, cancellationToken);
    }

    public async Task<FinancialCaseDto?> ResolveAsync(Guid id, string? rootCause, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, cancellationToken);
        if (item is null) return null;
        item.Resolve(rootCause);
        await cases.SaveAsync(cancellationToken);
        return await MapCaseAsync(item, cancellationToken);
    }

    public async Task<FinancialCaseDto?> IgnoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await cases.GetByIdAsync(id, cancellationToken);
        if (item is null) return null;
        item.Ignore();
        await cases.SaveAsync(cancellationToken);
        return await MapCaseAsync(item, cancellationToken);
    }

    public async Task<List<FinancialGraphRelationDto>> GetGraphAsync(
        FinancialNodeType nodeType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var relations = await graph.GetForNodeAsync(nodeType, entityId, cancellationToken);
        return relations.Select(x => new FinancialGraphRelationDto(
            x.Id,
            x.FromNodeType,
            x.FromEntityId,
            x.ToNodeType,
            x.ToEntityId,
            x.RelationType)).ToList();
    }

    private async Task EnsureFinancialCasesAsync(CancellationToken cancellationToken)
    {
        var discrepancyList = await discrepancies.GetAllAsync(cancellationToken);

        foreach (var discrepancy in discrepancyList)
        {
            if (discrepancy.Status is DiscrepancyStatus.Ignored or DiscrepancyStatus.Resolved)
                continue;

            if (await cases.ExistsOpenForDiscrepancyAsync(discrepancy.Id, cancellationToken))
                continue;

            var financialCase = FinancialCase.Create(
                discrepancy.Id,
                DetermineSeverity(discrepancy),
                discrepancy.Title,
                discrepancy.Description,
                discrepancy.AffectedAmount);

            await cases.AddAsync(financialCase, cancellationToken);

            var links = new List<FinancialCaseLink>
            {
                FinancialCaseLink.Create(
                    financialCase.Id,
                    FinancialNodeType.Discrepancy,
                    discrepancy.Id,
                    "Abweichung",
                    discrepancy.Title,
                    discrepancy.AffectedAmount)
            };

            if (discrepancy.OrderId.HasValue)
            {
                var order = await orders.GetByIdAsync(discrepancy.OrderId.Value, cancellationToken);
                if (order is not null)
                    links.Add(FinancialCaseLink.Create(
                        financialCase.Id,
                        FinancialNodeType.Order,
                        order.Id,
                        "Bestellung",
                        order.OrderNumber,
                        order.TotalAmount));
            }

            if (discrepancy.PaymentId.HasValue)
            {
                var payment = await payments.GetByIdAsync(discrepancy.PaymentId.Value, cancellationToken);
                if (payment is not null)
                    links.Add(FinancialCaseLink.Create(
                        financialCase.Id,
                        FinancialNodeType.Payment,
                        payment.Id,
                        "Zahlung",
                        payment.TransactionId,
                        payment.Amount));
            }

            if (discrepancy.InvoiceId.HasValue)
            {
                var invoice = await invoices.GetByIdAsync(discrepancy.InvoiceId.Value, cancellationToken);
                if (invoice is not null)
                    links.Add(FinancialCaseLink.Create(
                        financialCase.Id,
                        FinancialNodeType.Invoice,
                        invoice.Id,
                        "Rechnung",
                        invoice.InvoiceNumber,
                        invoice.Amount));
            }

            await caseLinks.AddRangeAsync(links, cancellationToken);
        }

        await cases.SaveAsync(cancellationToken);

        var orderList = await orders.GetAllAsync(cancellationToken);
        var paymentList = await payments.GetAllAsync(cancellationToken);

        foreach (var payment in paymentList)
        {
            var order = orderList.FirstOrDefault(x => x.Id == payment.OrderId);
            if (order is null) continue;

            if (!await graph.ExistsAsync(
                FinancialNodeType.Order, order.Id,
                FinancialNodeType.Payment, payment.Id,
                FinancialRelationType.PaymentFor,
                cancellationToken))
            {
                await graph.AddAsync(
                    FinancialGraphRelation.Create(
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

    private async Task<FinancialCaseDto> MapCaseAsync(FinancialCase item, CancellationToken cancellationToken)
    {
        var links = await caseLinks.GetForCaseAsync(item.Id, cancellationToken);
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

    private static decimal CalculateIntegrityScore(
        decimal revenue,
        decimal collected,
        decimal affectedAmount,
        IReadOnlyCollection<FinancialCase> openCases)
    {
        if (revenue <= 0) return 100;

        var collectionPenalty = Math.Min(25m, Math.Max(0, (revenue - collected) / revenue * 100m));
        var affectedPenalty = Math.Min(30m, affectedAmount / revenue * 100m * 0.5m);
        var casePenalty = Math.Min(20m, openCases.Count * 1.5m);
        var criticalPenalty = Math.Min(25m, openCases.Count(x => x.Severity == FinancialCaseSeverity.Critical) * 8m);

        return decimal.Round(Math.Clamp(100m - collectionPenalty - affectedPenalty - casePenalty - criticalPenalty, 0m, 100m), 1);
    }

    private static SystemHealthDto MapHealth(Domain.Entities.IntegrationConnection connection)
    {
        if (connection.Status == ConnectionStatus.Revoked)
            return new SystemHealthDto(connection.Id, connection.DisplayName, connection.Provider.ToString(), "Disconnected", "Verbindung wurde widerrufen.", connection.LastSeenAtUtc);

        if (connection.LastSeenAtUtc is null)
            return new SystemHealthDto(connection.Id, connection.DisplayName, connection.Provider.ToString(), "Warning", "Noch kein Ereignis empfangen.", null);

        var age = DateTime.UtcNow - connection.LastSeenAtUtc.Value;
        if (age <= TimeSpan.FromHours(24))
            return new SystemHealthDto(connection.Id, connection.DisplayName, connection.Provider.ToString(), "Healthy", null, connection.LastSeenAtUtc);

        return new SystemHealthDto(connection.Id, connection.DisplayName, connection.Provider.ToString(), "Warning", "Seit mehr als 24 Stunden kein Ereignis empfangen.", connection.LastSeenAtUtc);
    }
}
