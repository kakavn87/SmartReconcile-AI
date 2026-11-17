import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AiAnalysisRequest, AiAnalysisResponse, ControlTower, Dashboard, Discrepancy, FinancialCase, FinancialGraphRelation, Invoice, Order, Payment, ReconciliationResult, FinancialNodeType } from '../models/api.models';

export const API_BASE_URL = 'https://smartreconcile-ai-api.onrender.com/api';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  dashboard() { return this.http.get<Dashboard>(API_BASE_URL + '/dashboard'); }
  controlTower() { return this.http.get<ControlTower>(API_BASE_URL + '/control-tower'); }
  financialCase(id:string) { return this.http.get<FinancialCase>(API_BASE_URL + '/control-tower/cases/' + id); }
  investigateCase(id:string) { return this.http.post<FinancialCase>(API_BASE_URL + '/control-tower/cases/' + id + '/investigate', {}); }
  resolveCase(id:string, rootCause:string|null) { return this.http.post<FinancialCase>(API_BASE_URL + '/control-tower/cases/' + id + '/resolve', { rootCause }); }
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
  health() { return this.http.get(API_BASE_URL.replace('/api','') + '/health', { responseType:'text' }); }
}
