import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ApiService } from '../../core/api/api.service';
import { Invoice } from '../../core/models/api.models';

@Component({
  standalone:true, imports:[CurrencyPipe,DatePipe], changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <div class="mb-8"><h1 class="text-2xl font-bold tracking-tight">Rechnungen</h1><p class="mt-1 text-sm text-slate-500">Rechnungen und Lieferantenbeträge.</p></div>
    @if(loading){<div class="app-card p-8 text-sm text-slate-500">Rechnungen werden geladen…</div>}
    @if(error){<div class="mb-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{{error}}</div>}
    @if(!loading){<div class="app-card overflow-hidden"><div class="overflow-x-auto"><table class="min-w-full text-sm">
      <thead class="border-b border-slate-200 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500"><tr><th class="px-5 py-3">Rechnung</th><th class="px-5 py-3">Lieferant</th><th class="px-5 py-3">Betrag</th><th class="px-5 py-3">Rechnungsdatum</th></tr></thead>
      <tbody class="divide-y divide-slate-100">@for(item of items;track item.id){<tr><td class="px-5 py-4 font-semibold">{{item.invoiceNumber}}</td><td class="px-5 py-4">{{item.supplierName}}</td><td class="px-5 py-4 font-semibold">{{item.amount|currency:item.currency:'symbol':'1.2-2':'de-DE'}}</td><td class="px-5 py-4 text-slate-500">{{item.invoiceDateUtc|date:'dd.MM.yyyy'}}</td></tr>}</tbody>
    </table></div></div>}
  `
})
export class InvoicesPage {
  private readonly api=inject(ApiService); items:Invoice[]=[]; loading=true; error:string|null=null;
  constructor(){this.api.invoices().subscribe({next:x=>{this.items=x;this.loading=false},error:()=>{this.error='Rechnungen konnten nicht geladen werden.';this.loading=false}})}
}