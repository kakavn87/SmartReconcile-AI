import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AiAnalysisRequest, AiAnalysisResponse, AccountingSettings, ControlTower, Dashboard, Discrepancy, FinancialCase, FinancialCaseDetail, FinancialGraphRelation, Invoice, Order, Payment, ReconciliationResult, FinancialNodeType, UpdateAccountingSettingsRequest, IntegrationConnection, ShopifySyncResult } from '../models/api.models';

export const API_BASE_URL = 'https://smartreconcile-ai-api.onrender.com/api';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  dashboard() { return this.http.get<Dashboard>(API_BASE_URL + '/dashboard'); }
  controlTower() { return this.http.get<ControlTower>(API_BASE_URL + '/control-tower'); }
  financialCase(id:string) { return this.http.get<FinancialCaseDetail>(API_BASE_URL + '/control-tower/cases/' + id); }
  autoFixCase(id:string) { return this.http.post<FinancialCase>(API_BASE_URL + '/control-tower/cases/' + id + '/auto-fix', {}); }
  investigateCase(id:string) { return this.http.post<FinancialCase>(API_BASE_URL + '/control-tower/cases/' + id + '/investigate', {}); }
  resolveCase(id:string, rootCause:string|null, note:string|null = null) { return this.http.post<FinancialCase>(API_BASE_URL + '/control-tower/cases/' + id + '/resolve', { rootCause, note }); }
  ignoreCase(id:string) { return this.http.post<FinancialCase>(API_BASE_URL + '/control-tower/cases/' + id + '/ignore', {}); }
  financialGraph(nodeType:FinancialNodeType, entityId:string) { return this.http.get<FinancialGraphRelation[]>(API_BASE_URL + '/control-tower/graph/' + nodeType + '/' + entityId); }
  discrepancies() { return this.http.get<Discrepancy[]>(API_BASE_URL + '/discrepancies'); }
  updateDiscrepancyStatus(id:string, status:'Open'|'Resolved'|'Ignored') { return this.http.patch<Discrepancy>(API_BASE_URL + '/discrepancies/' + id + '/status', { status }); }
  orders() { return this.http.get<Order[]>(API_BASE_URL + '/orders'); }
  payments() { return this.http.get<Payment[]>(API_BASE_URL + '/payments'); }
  invoices() { return this.http.get<Invoice[]>(API_BASE_URL + '/invoices'); }
  analyzeInvoice(request:AiAnalysisRequest) { return this.http.post<AiAnalysisResponse>(API_BASE_URL + '/invoices/analyze', request); }
  reconciliation() { return this.http.post<ReconciliationResult>(API_BASE_URL + '/reconciliation/run', {}); }
  exportCsv() { return this.http.get(API_BASE_URL + '/export/csv', { responseType:'blob' }); }
  accountingSettings() { return this.http.get<AccountingSettings>(API_BASE_URL + '/settings/accounting'); }
  updateAccountingSettings(request:UpdateAccountingSettingsRequest) { return this.http.put<AccountingSettings>(API_BASE_URL + '/settings/accounting', request); }
  integrationConnections(tenantId:string) { return this.http.get<IntegrationConnection[]>(API_BASE_URL + '/v1/integrations/tenants/' + tenantId + '/connections'); }
  shopifySession() { return this.http.get<import('../models/api.models').ShopifySessionInfo>(API_BASE_URL + '/auth/shopify/session'); }
  currentTenantIntegrationConnections() { return this.http.get<IntegrationConnection[]>(API_BASE_URL + '/v1/integrations/connections'); }
  syncShopify(connectionId:string) { return this.http.post<ShopifySyncResult>(API_BASE_URL + '/integrations/shopify/' + connectionId + '/sync', {}); }
  exportDatev(from:string, to:string, onlyReconciled:boolean) {
    const params = { from, to, onlyReconciled };
    return this.http.get(API_BASE_URL + '/export/datev', { params, responseType:'blob', observe:'response' });
  }
  health() { return this.http.get(API_BASE_URL.replace('/api','') + '/health', { responseType:'text' }); }
}
