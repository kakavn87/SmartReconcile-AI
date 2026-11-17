import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AiAnalysisRequest, AiAnalysisResponse, Dashboard, Discrepancy, Invoice, Order, Payment, ReconciliationResult } from '../models/api.models';

export const API_BASE_URL = 'https://smartreconcile-ai-api.onrender.com/api';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  dashboard() { return this.http.get<Dashboard>(API_BASE_URL + '/dashboard'); }
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
